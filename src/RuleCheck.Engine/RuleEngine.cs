using System.Collections.Frozen;
using RuleCheck.Core.Issues;
using RuleCheck.Core.Rules;
using RuleCheck.Core.Snapshot;
using RuleCheck.Engine.Evaluators;

namespace RuleCheck.Engine;

/// <summary>
/// The stateless rule-check orchestrator. Given a <see cref="ModelSnapshot"/> and a
/// <see cref="RuleSet"/>, it targets elements, evaluates checks, and returns a
/// <see cref="CheckReport"/>. Has zero Revit dependencies — all data comes through
/// the snapshot contract.
/// </summary>
public sealed class RuleEngine
{
    private readonly FrozenDictionary<CheckType, ICheckEvaluator> _evaluators;

    public RuleEngine() : this(DefaultEvaluators()) { }

    public RuleEngine(IEnumerable<ICheckEvaluator> evaluators)
    {
        _evaluators = evaluators.ToFrozenDictionary(e => e.Type);
    }

    /// <summary>Evaluate all expressible rules against the snapshot. Pure function.</summary>
    public CheckReport Run(ModelSnapshot model, RuleSet ruleSet)
    {
        var context = new EvaluationContext(model);
        var issues = new List<Issue>();
        var ruleErrors = new List<RuleError>();
        int evaluated = 0, skipped = 0;

        foreach (var rule in ruleSet.Rules)
        {
            // Skip rules that cannot be evaluated.
            if (rule.Status != RuleStatus.Ok || rule.Check is null || rule.Target is null)
            {
                skipped++;
                continue;
            }

            if (!_evaluators.TryGetValue(rule.Check.Type, out var evaluator))
            {
                ruleErrors.Add(new RuleError(rule.Id, $"No evaluator registered for check type '{rule.Check.Type}'."));
                skipped++;
                continue;
            }

            // Step 1: Filter elements by category and element kind.
            var targets = FilterTargets(model.Elements, rule.Target, context);

            if (targets.Count == 0)
            {
                skipped++;
                continue;
            }

            // Step 2: Apply where-conditions.
            if (rule.Target.Where is { Count: > 0 } conditions)
            {
                targets = targets
                    .Where(e => ConditionEvaluator.MatchesAll(conditions, e, context))
                    .ToList();
            }

            // Step 3: Evaluate.
            try
            {
                issues.AddRange(evaluator.Evaluate(rule, targets, context));
                evaluated++;
            }
            catch (RuleConfigurationException ex)
            {
                ruleErrors.Add(new RuleError(rule.Id, ex.Message));
                skipped++;
            }
        }

        return new CheckReport
        {
            ModelTitle = model.ModelTitle,
            RunAt = DateTimeOffset.UtcNow,
            RulesEvaluated = evaluated,
            RulesSkipped = skipped,
            ElementsScanned = model.Elements.Count,
            Issues = issues,
            RuleErrors = ruleErrors,
        };
    }

    // ─── internal helpers ────────────────────────────────────────────────

    private static List<ElementSnapshot> FilterTargets(
        IReadOnlyList<ElementSnapshot> allElements,
        RuleTarget target,
        EvaluationContext context)
    {
        var categorySet = new HashSet<string>(target.Categories, StringComparer.OrdinalIgnoreCase);
        var result = new List<ElementSnapshot>();

        foreach (var e in allElements)
        {
            if (!categorySet.Contains(e.Category)) continue;

            switch (target.ElementKind)
            {
                case ElementKind.Instance when e.IsType: continue;
                case ElementKind.Type when !e.IsType: continue;
            }

            result.Add(e);
        }

        return result;
    }

    private static IEnumerable<ICheckEvaluator> DefaultEvaluators() =>
    [
        new RequiredParameterEvaluator(),
        new PatternEvaluator(),
        new AllowedValuesEvaluator(),
        new RangeEvaluator(),
        new UniqueEvaluator(),
        new PlacementEvaluator(),
        new NotUnusedEvaluator(),
    ];
}
