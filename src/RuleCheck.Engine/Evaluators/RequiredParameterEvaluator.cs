using RuleCheck.Core.Issues;
using RuleCheck.Core.Rules;
using RuleCheck.Core.Snapshot;

namespace RuleCheck.Engine.Evaluators;

/// <summary>Fails if the parameter is missing or has an empty/null value.</summary>
public sealed class RequiredParameterEvaluator : ICheckEvaluator
{
    public CheckType Type => CheckType.RequiredParameter;

    public IEnumerable<Issue> Evaluate(RuleDefinition rule, IReadOnlyList<ElementSnapshot> targets, EvaluationContext context)
    {
        var param = rule.Check!.Parameter
            ?? throw new RuleConfigurationException("required_parameter check needs 'parameter'.");

        foreach (var element in targets)
        {
            var value = context.Resolve(element, param);
            if (value is null || Values.IsEmpty(value))
                yield return EvaluationContext.CreateIssue(rule, element,
                    $"Required parameter '{param}' is missing or empty.");
        }
    }
}
