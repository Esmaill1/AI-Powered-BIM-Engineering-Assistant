using RuleCheck.Core.Issues;
using RuleCheck.Core.Rules;
using RuleCheck.Core.Snapshot;

namespace RuleCheck.Engine.Evaluators;

/// <summary>Fails if the parameter value is not in the list of allowed values (string comparison).</summary>
public sealed class AllowedValuesEvaluator : ICheckEvaluator
{
    public CheckType Type => CheckType.AllowedValues;

    public IEnumerable<Issue> Evaluate(RuleDefinition rule, IReadOnlyList<ElementSnapshot> targets, EvaluationContext context)
    {
        var check = rule.Check!;
        var param = check.Parameter
            ?? throw new RuleConfigurationException("allowed_values check needs 'parameter'.");
        var allowed = check.Values
            ?? throw new RuleConfigurationException("allowed_values check needs 'values'.");

        var comparison = check.CaseSensitive is true
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        var set = new HashSet<string>(allowed, check.CaseSensitive is true
            ? StringComparer.Ordinal
            : StringComparer.OrdinalIgnoreCase);

        foreach (var element in targets)
        {
            var value = context.Resolve(element, param);
            if (value is null || Values.IsEmpty(value))
            {
                yield return EvaluationContext.CreateIssue(rule, element,
                    $"Parameter '{param}' is missing (expected one of [{string.Join(", ", allowed)}]).");
                continue;
            }

            var text = Values.Text(value).Trim();
            if (!set.Contains(text))
                yield return EvaluationContext.CreateIssue(rule, element,
                    $"Parameter '{param}' value '{text}' is not in the allowed list [{string.Join(", ", allowed)}].");
        }
    }
}
