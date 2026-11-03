using System.Globalization;
using RuleCheck.Core.Issues;
using RuleCheck.Core.Rules;
using RuleCheck.Core.Snapshot;

namespace RuleCheck.Engine;

/// <summary>
/// Per-run lookups shared by evaluators: element index, type usage, levels and rooms,
/// and instance→type parameter resolution.
/// </summary>
public sealed class EvaluationContext
{
    private readonly Dictionary<long, ElementSnapshot> _byId;

    public EvaluationContext(ModelSnapshot model)
    {
        Model = model;
        _byId = new Dictionary<long, ElementSnapshot>(model.Elements.Count);
        foreach (var e in model.Elements) _byId.TryAdd(e.ElementId, e);

        UsedTypeIds = model.Elements.Where(e => !e.IsType && e.TypeId is not null)
                                    .Select(e => e.TypeId!.Value).ToHashSet();
        LevelIds = model.Levels.Select(l => l.Id).ToHashSet();
        RoomIds = model.Rooms.Select(r => r.Id).ToHashSet();
    }

    public ModelSnapshot Model { get; }
    public IReadOnlySet<long> UsedTypeIds { get; }
    public IReadOnlySet<long> LevelIds { get; }
    public IReadOnlySet<long> RoomIds { get; }

    /// <summary>
    /// Looks up a parameter on the element; for instances, falls back to the type element.
    /// Returns null if the parameter does not exist on either.
    /// </summary>
    public ParamValue? Resolve(ElementSnapshot element, string parameter)
    {
        if (element.Parameters.TryGetValue(parameter, out var value)) return value;
        if (!element.IsType && element.TypeId is long typeId &&
            _byId.TryGetValue(typeId, out var type) &&
            type.Parameters.TryGetValue(parameter, out var typeValue))
            return typeValue;
        return null;
    }

    public static Issue CreateIssue(RuleDefinition rule, ElementSnapshot element, string message) =>
        new(rule.Id, rule.Name, rule.Check!.Type, rule.Severity, element.ElementId, element.Category, element.LevelId, message);
}

/// <summary>Value helpers shared by conditions and checks. Pure and culture-invariant.</summary>
public static class Values
{
    public const double Tolerance = 1e-6;

    public static bool IsEmpty(ParamValue v) =>
        !v.HasValue
        || v.Storage == StorageKind.None
        || (v.Storage == StorageKind.String && string.IsNullOrWhiteSpace(v.AsString))
        || (v.Storage == StorageKind.ElementId && (v.AsElementId is null or -1));

    public static bool TryGetNumber(ParamValue v, out double number)
    {
        switch (v.Storage)
        {
            case StorageKind.Double when v.HasValue && v.AsDouble is double d: number = d; return true;
            case StorageKind.Integer when v.HasValue && v.AsInteger is long i: number = i; return true;
            default: number = 0; return false;
        }
    }

    /// <summary>Text used for pattern/allowed-value/uniqueness comparisons.</summary>
    public static string Text(ParamValue v) => v.Storage switch
    {
        StorageKind.String => v.AsString ?? "",
        _ when !string.IsNullOrEmpty(v.Display) => v.Display!,
        StorageKind.Double => v.AsDouble?.ToString("R", CultureInfo.InvariantCulture) ?? "",
        StorageKind.Integer => v.AsInteger?.ToString(CultureInfo.InvariantCulture) ?? "",
        StorageKind.ElementId => v.AsElementId?.ToString(CultureInfo.InvariantCulture) ?? "",
        _ => "",
    };

    public static bool NearlyEqual(double a, double b) => Math.Abs(a - b) <= Tolerance;

    public static string Describe(ParamValue v) => IsEmpty(v) ? "<empty>" : $"'{Text(v)}'";
}
