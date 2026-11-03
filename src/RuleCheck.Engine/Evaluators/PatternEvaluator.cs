using System.Text.RegularExpressions;
using RuleCheck.Core.Issues;
using RuleCheck.Core.Rules;
using RuleCheck.Core.Snapshot;

namespace RuleCheck.Engine.Evaluators;

/// <summary>Fails if the parameter value does not fully match the regex pattern.</summary>
public sealed class PatternEvaluator : ICheckEvaluator
{
    public CheckType Type => CheckType.Pattern;

    public IEnumerable<Issue> Evaluate(RuleDefinition rule, IReadOnlyList<ElementSnapshot> targets, EvaluationContext context)
    {
        var check = rule.Check!;
        var param = check.Parameter
            ?? throw new RuleConfigurationException("pattern check needs 'parameter'.");
        var pattern = check.Regex
            ?? throw new RuleConfigurationException("pattern check needs 'regex'.");

        var options = RegexOptions.Singleline | RegexOptions.CultureInvariant;
        if (check.CaseSensitive is null or false)
            options |= RegexOptions.IgnoreCase;

        var regex = new Regex($"^(?:{pattern})$", options, TimeSpan.FromSeconds(2));

        foreach (var element in targets)
        {
            var value = context.Resolve(element, param);
            if (value is null || Values.IsEmpty(value))
            {
                yield return EvaluationContext.CreateIssue(rule, element,
                    $"Parameter '{param}' is missing (expected to match pattern '{pattern}').");
                continue;
            }

            var text = Values.Text(value);
            if (!regex.IsMatch(text))
                yield return EvaluationContext.CreateIssue(rule, element,
                    $"Parameter '{param}' value '{text}' does not match pattern '{pattern}'.");
        }
    }
}
