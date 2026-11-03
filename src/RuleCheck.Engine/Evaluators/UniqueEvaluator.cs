using RuleCheck.Core.Issues;
using RuleCheck.Core.Rules;
using RuleCheck.Core.Snapshot;

namespace RuleCheck.Engine.Evaluators;

/// <summary>
/// Fails if the parameter value is not unique within the given scope (model / category / level).
/// Scope defaults to <see cref="UniqueScope.Category"/>.
/// </summary>
public sealed class UniqueEvaluator : ICheckEvaluator
{
    public CheckType Type => CheckType.Unique;

    public IEnumerable<Issue> Evaluate(RuleDefinition rule, IReadOnlyList<ElementSnapshot> targets, EvaluationContext context)
    {
        var check = rule.Check!;
        var param = check.Parameter
            ?? throw new RuleConfigurationException("unique check needs 'parameter'.");

        var scope = check.Scope ?? UniqueScope.Category;
        var caseSensitive = check.CaseSensitive is true;
        var comparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

        // Group by scope key, then find duplicates within each group.
        var groups = targets.GroupBy(e => ScopeKey(e, scope));

        foreach (var group in groups)
        {
            var seen = new Dictionary<string, List<ElementSnapshot>>(comparer);
            foreach (var element in group)
            {
                var value = context.Resolve(element, param);
                if (value is null || Values.IsEmpty(value))
                    continue; // empty values are not considered duplicates

                var text = Values.Text(value).Trim();
                if (!seen.TryGetValue(text, out var list))
                    seen[text] = list = [];
                list.Add(element);
            }

            foreach (var (text, elements) in seen.Where(kv => kv.Value.Count > 1))
            {
                foreach (var element in elements)
                {
                    yield return EvaluationContext.CreateIssue(rule, element,
                        $"Parameter '{param}' value '{text}' is duplicated ({elements.Count} occurrences in scope '{scope}').");
                }
            }
        }
    }

    private static string ScopeKey(ElementSnapshot e, UniqueScope scope) => scope switch
    {
        UniqueScope.Model => "*",
        UniqueScope.Category => e.Category,
        UniqueScope.Level => e.LevelId?.ToString() ?? "none",
        _ => throw new RuleConfigurationException($"Unknown scope '{scope}'."),
    };
}
