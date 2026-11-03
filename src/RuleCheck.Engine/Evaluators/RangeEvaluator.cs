using System.Globalization;
using RuleCheck.Core.Issues;
using RuleCheck.Core.Rules;
using RuleCheck.Core.Snapshot;
using RuleCheck.Core.Units;

namespace RuleCheck.Engine.Evaluators;

/// <summary>Fails if the numeric parameter value is outside [min, max]. At least one bound is required.</summary>
public sealed class RangeEvaluator : ICheckEvaluator
{
    public CheckType Type => CheckType.Range;

    public IEnumerable<Issue> Evaluate(RuleDefinition rule, IReadOnlyList<ElementSnapshot> targets, EvaluationContext context)
    {
        var check = rule.Check!;
        var param = check.Parameter
            ?? throw new RuleConfigurationException("range check needs 'parameter'.");
        if (check.Min is null && check.Max is null)
            throw new RuleConfigurationException("range check needs at least 'min' or 'max'.");

        var unit = check.Unit;
        var min = check.Min is double m ? UnitConverter.ToInternal(m, unit) : (double?)null;
        var max = check.Max is double x ? UnitConverter.ToInternal(x, unit) : (double?)null;

        foreach (var element in targets)
        {
            var value = context.Resolve(element, param);
            if (value is null || Values.IsEmpty(value))
            {
                yield return EvaluationContext.CreateIssue(rule, element,
                    $"Parameter '{param}' is missing (expected numeric value in range).");
                continue;
            }

            if (!Values.TryGetNumber(value, out var actual))
            {
                yield return EvaluationContext.CreateIssue(rule, element,
                    $"Parameter '{param}' value {Values.Describe(value)} is not numeric.");
                continue;
            }

            if (min is double lo && actual < lo - Values.Tolerance)
            {
                yield return EvaluationContext.CreateIssue(rule, element,
                    $"Parameter '{param}' value {FormatValue(actual, unit)} is below minimum {FormatThreshold(check.Min!.Value, unit)}.");
                continue;
            }

            if (max is double hi && actual > hi + Values.Tolerance)
            {
                yield return EvaluationContext.CreateIssue(rule, element,
                    $"Parameter '{param}' value {FormatValue(actual, unit)} exceeds maximum {FormatThreshold(check.Max!.Value, unit)}.");
            }
        }
    }

    private static string FormatThreshold(double value, string? unit) =>
        unit is not null
            ? $"{value.ToString("G", CultureInfo.InvariantCulture)} {unit}"
            : value.ToString("G", CultureInfo.InvariantCulture);

    private static string FormatValue(double internalValue, string? unit) =>
        internalValue.ToString("G6", CultureInfo.InvariantCulture) + " (internal)";
}
