using RuleCheck.Core.Issues;
using RuleCheck.Core.Rules;
using RuleCheck.Core.Snapshot;

namespace RuleCheck.Engine.Evaluators;

/// <summary>Fails if an element is not assigned to a room or level (depending on <c>placement_kind</c>).</summary>
public sealed class PlacementEvaluator : ICheckEvaluator
{
    public CheckType Type => CheckType.Placement;

    public IEnumerable<Issue> Evaluate(RuleDefinition rule, IReadOnlyList<ElementSnapshot> targets, EvaluationContext context)
    {
        var kind = rule.Check!.PlacementKind
            ?? throw new RuleConfigurationException("placement check needs 'placement_kind'.");

        foreach (var element in targets)
        {
            switch (kind)
            {
                case PlacementKind.RoomAssigned when element.RoomId is null:
                    yield return EvaluationContext.CreateIssue(rule, element,
                        "Element is not placed in any room.");
                    break;

                case PlacementKind.LevelAssigned when element.LevelId is null:
                    yield return EvaluationContext.CreateIssue(rule, element,
                        "Element is not assigned to any level.");
                    break;
            }
        }
    }
}
