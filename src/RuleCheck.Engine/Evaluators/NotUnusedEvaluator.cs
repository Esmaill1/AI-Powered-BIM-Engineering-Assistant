using RuleCheck.Core.Issues;
using RuleCheck.Core.Rules;
using RuleCheck.Core.Snapshot;

namespace RuleCheck.Engine.Evaluators;

/// <summary>
/// Fails on type elements that are not used by any instance in the model.
/// This catches unused families, wall types, etc. that should be cleaned up.
/// </summary>
public sealed class NotUnusedEvaluator : ICheckEvaluator
{
    public CheckType Type => CheckType.NotUnused;

    public IEnumerable<Issue> Evaluate(RuleDefinition rule, IReadOnlyList<ElementSnapshot> targets, EvaluationContext context)
    {
        foreach (var element in targets)
        {
            // Only applies to type elements.
            if (!element.IsType) continue;

            if (!context.UsedTypeIds.Contains(element.ElementId))
                yield return EvaluationContext.CreateIssue(rule, element,
                    $"Type element '{Values.Text(element.Parameters.GetValueOrDefault("Type Name") ?? new())}' " +
                    $"in category '{element.Category}' is not used by any instance.");
        }
    }
}
