using RuleCheck.Core.Snapshot;

namespace RuleCheck.Core.Vocabulary;

/// <summary>
/// Category → parameter names (+ storage kinds) present in a model. Contains <b>no values</b>,
/// so it is safe to send to the LLM for grounding. Includes type parameters, because type
/// elements share their instances' category.
/// </summary>
public sealed class ModelVocabulary
{
    private readonly SortedDictionary<string, SortedDictionary<string, StorageKind>> _byCategory;

    private ModelVocabulary(SortedDictionary<string, SortedDictionary<string, StorageKind>> byCategory) =>
        _byCategory = byCategory;

    public IEnumerable<string> Categories => _byCategory.Keys;

    public static ModelVocabulary FromSnapshot(ModelSnapshot snapshot)
    {
        var map = new SortedDictionary<string, SortedDictionary<string, StorageKind>>(StringComparer.Ordinal);
        foreach (var element in snapshot.Elements)
        {
            if (!map.TryGetValue(element.Category, out var parameters))
                map[element.Category] = parameters = new SortedDictionary<string, StorageKind>(StringComparer.Ordinal);
            foreach (var (name, value) in element.Parameters)
                parameters.TryAdd(name, value.Storage);
        }
        return new ModelVocabulary(map);
    }

    public bool HasCategory(string category) => _byCategory.ContainsKey(category);

    public bool HasParameter(string category, string parameter) =>
        _byCategory.TryGetValue(category, out var p) && p.ContainsKey(parameter);

    public IReadOnlyDictionary<string, StorageKind> ParametersOf(string category) =>
        _byCategory.TryGetValue(category, out var p) ? p : new SortedDictionary<string, StorageKind>();

    /// <summary>Union of parameters across the given categories (used to build per-rule LLM enums).</summary>
    public IReadOnlyCollection<string> ParametersOf(IEnumerable<string> categories)
    {
        var set = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var c in categories) set.UnionWith(ParametersOf(c).Keys);
        return set;
    }
}
