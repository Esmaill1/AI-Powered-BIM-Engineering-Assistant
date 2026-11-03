using System.Globalization;
using RuleCheck.Core.Rules;
using RuleCheck.Core.Snapshot;
using RuleCheck.Core.Units;

namespace RuleCheck.Engine;

/// <summary>
/// Evaluates where-conditions. Semantics (see docs/engine-semantics.md):
/// a missing parameter makes the condition false; text comparisons are case-insensitive;
/// numeric comparisons convert the rule value from its unit into Revit internal units.
/// </summary>
public static class ConditionEvaluator
{
    public static bool MatchesAll(IReadOnlyList<WhereCondition>? conditions, ElementSnapshot element, EvaluationContext context)
    {
        if (conditions is null) return true;
        foreach (var c in conditions)
        {
            var value = context.Resolve(element, c.Parameter);
            if (value is null || !Matches(c, value)) return false;
        }
        return true;
    }

    public static bool Matches(WhereCondition c, ParamValue value) => c.Operator switch
    {
        ConditionOperator.Equals => ValueEquals(value, c.Value, c.Unit),
        ConditionOperator.NotEquals => !ValueEquals(value, c.Value, c.Unit),
        ConditionOperator.Contains => Values.Text(value).Contains(c.Value.ToString(), StringComparison.OrdinalIgnoreCase),
        ConditionOperator.In => c.Value.Kind == RuleValueKind.List
            ? c.Value.Items!.Any(item => ValueEquals(value, item, c.Unit))
            : ValueEquals(value, c.Value, c.Unit),
        ConditionOperator.GreaterThan => CompareNumber(value, c, out var cmp) && cmp > 0,
        ConditionOperator.LessThan => CompareNumber(value, c, out var cmp) && cmp < 0,
        _ => throw new RuleConfigurationException($"Unknown operator '{c.Operator}'."),
    };

    private static bool CompareNumber(ParamValue value, WhereCondition c, out int comparison)
    {
        comparison = 0;
        if (!Values.TryGetNumber(value, out var actual)) return false;
        double threshold;
        if (c.Value.Kind == RuleValueKind.Number) threshold = c.Value.Number!.Value;
        else if (c.Value.Kind == RuleValueKind.Text &&
                 double.TryParse(c.Value.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) threshold = parsed;
        else return false;

        threshold = UnitConverter.ToInternal(threshold, c.Unit);
        comparison = Values.NearlyEqual(actual, threshold) ? 0 : actual.CompareTo(threshold);
        return true;
    }

    private static bool ValueEquals(ParamValue value, RuleValue expected, string? unit)
    {
        switch (expected.Kind)
        {
            case RuleValueKind.Boolean:
                // Revit Yes/No parameters are integers (0/1).
                return Values.TryGetNumber(value, out var flag) && (flag != 0) == expected.Boolean;

            case RuleValueKind.Number:
                return Values.TryGetNumber(value, out var n)
                    ? Values.NearlyEqual(n, UnitConverter.ToInternal(expected.Number!.Value, unit))
                    : string.Equals(Values.Text(value), expected.ToString(), StringComparison.OrdinalIgnoreCase);

            case RuleValueKind.Text:
                if (string.Equals(Values.Text(value).Trim(), expected.Text!.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;
                return Values.TryGetNumber(value, out var num)
                       && double.TryParse(expected.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
                       && Values.NearlyEqual(num, UnitConverter.ToInternal(d, unit));

            default:
                return expected.Items!.Any(i => ValueEquals(value, i, unit));
        }
    }
}
