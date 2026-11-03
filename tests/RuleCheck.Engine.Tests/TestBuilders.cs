using RuleCheck.Core.Issues;
using RuleCheck.Core.Rules;
using RuleCheck.Core.Snapshot;

namespace RuleCheck.Engine.Tests;

/// <summary>Fluent builders for creating test snapshots, rules, and elements without ceremony.</summary>
internal static class TestBuilders
{
    // ── Elements ─────────────────────────────────────────────────────────
    public static ElementSnapshot Elem(
        long id,
        string category = "Walls",
        bool isType = false,
        long? typeId = null,
        long? levelId = null,
        long? roomId = null,
        params (string Name, ParamValue Value)[] parameters)
    {
        var dict = new Dictionary<string, ParamValue>(StringComparer.Ordinal);
        foreach (var (name, value) in parameters) dict[name] = value;
        return new ElementSnapshot
        {
            ElementId = id,
            Category = category,
            IsType = isType,
            TypeId = typeId,
            LevelId = levelId,
            RoomId = roomId,
            Parameters = dict,
        };
    }

    // ── Snapshot ──────────────────────────────────────────────────────────
    public static ModelSnapshot Snap(params ElementSnapshot[] elements) =>
        new() { ModelTitle = "Test Model", Elements = elements };

    // ── Rules ────────────────────────────────────────────────────────────
    public static RuleDefinition RequiredParamRule(
        string id = "R-001",
        string parameter = "Mark",
        string category = "Walls",
        Severity severity = Severity.Error) =>
        new()
        {
            Id = id,
            Name = $"Test Rule {id}",
            Severity = severity,
            Target = new RuleTarget { Categories = [category] },
            Check = new CheckSpec { Type = CheckType.RequiredParameter, Parameter = parameter },
        };

    public static RuleDefinition PatternRule(
        string id = "R-002",
        string parameter = "Mark",
        string regex = @"W-\d+",
        string category = "Walls") =>
        new()
        {
            Id = id,
            Name = $"Test Rule {id}",
            Target = new RuleTarget { Categories = [category] },
            Check = new CheckSpec { Type = CheckType.Pattern, Parameter = parameter, Regex = regex },
        };

    public static RuleDefinition AllowedValuesRule(
        string id = "R-003",
        string parameter = "Fire Rating",
        IReadOnlyList<string>? values = null,
        string category = "Walls") =>
        new()
        {
            Id = id,
            Name = $"Test Rule {id}",
            Target = new RuleTarget { Categories = [category] },
            Check = new CheckSpec { Type = CheckType.AllowedValues, Parameter = parameter, Values = values ?? ["1HR", "2HR", "3HR"] },
        };

    public static RuleDefinition RangeRule(
        string id = "R-004",
        string parameter = "Width",
        double? min = null,
        double? max = null,
        string? unit = null,
        string category = "Walls") =>
        new()
        {
            Id = id,
            Name = $"Test Rule {id}",
            Target = new RuleTarget { Categories = [category] },
            Check = new CheckSpec { Type = CheckType.Range, Parameter = parameter, Min = min, Max = max, Unit = unit },
        };

    public static RuleDefinition UniqueRule(
        string id = "R-005",
        string parameter = "Mark",
        UniqueScope scope = UniqueScope.Category,
        string category = "Walls") =>
        new()
        {
            Id = id,
            Name = $"Test Rule {id}",
            Target = new RuleTarget { Categories = [category] },
            Check = new CheckSpec { Type = CheckType.Unique, Parameter = parameter, Scope = scope },
        };

    public static RuleDefinition PlacementRule(
        string id = "R-006",
        PlacementKind kind = PlacementKind.RoomAssigned,
        string category = "Furniture") =>
        new()
        {
            Id = id,
            Name = $"Test Rule {id}",
            Target = new RuleTarget { Categories = [category] },
            Check = new CheckSpec { Type = CheckType.Placement, PlacementKind = kind },
        };

    public static RuleDefinition NotUnusedRule(
        string id = "R-007",
        string category = "Walls") =>
        new()
        {
            Id = id,
            Name = $"Test Rule {id}",
            Target = new RuleTarget { Categories = [category], ElementKind = ElementKind.Type },
            Check = new CheckSpec { Type = CheckType.NotUnused },
        };

    public static RuleSet Set(params RuleDefinition[] rules) =>
        new() { Name = "Test Set", Rules = rules };
}
