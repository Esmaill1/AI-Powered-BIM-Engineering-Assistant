namespace RuleCheck.Core.Snapshot;

/// <summary>
/// Read-only, Revit-independent copy of the model data the engine needs.
/// Produced by the Revit add-in (SnapshotExtractor) and serializable to JSON so the
/// engine, LLM pipeline and evaluation harness can run without Revit.
/// </summary>
public sealed record ModelSnapshot
{
    public const string CurrentSchemaVersion = "1.0";

    public string SchemaVersion { get; init; } = CurrentSchemaVersion;
    public string ModelTitle { get; init; } = "";
    public string RevitVersion { get; init; } = "";
    public DateTimeOffset ExtractedAt { get; init; }
    public IReadOnlyList<LevelInfo> Levels { get; init; } = [];
    public IReadOnlyList<RoomInfo> Rooms { get; init; } = [];
    public IReadOnlyList<ElementSnapshot> Elements { get; init; } = [];
}

public sealed record LevelInfo(long Id, string Name, double Elevation);

public sealed record RoomInfo(long Id, string Name, string Number, long? LevelId);

/// <summary>
/// One Revit element (instance or type). Element IDs are 64-bit (Revit 2024+ <c>ElementId.Value</c>).
/// </summary>
public sealed record ElementSnapshot
{
    public required long ElementId { get; init; }
    public string UniqueId { get; init; } = "";
    public required string Category { get; init; }
    public bool IsType { get; init; }

    /// <summary>For instances: the ID of their type element, if any.</summary>
    public long? TypeId { get; init; }

    public long? LevelId { get; init; }
    public long? RoomId { get; init; }

    /// <summary>Parameters keyed by their display name (as shown in Revit's Properties palette).</summary>
    public IReadOnlyDictionary<string, ParamValue> Parameters { get; init; } =
        new Dictionary<string, ParamValue>(StringComparer.Ordinal);
}

/// <summary>Mirrors Revit's <c>StorageType</c>.</summary>
public enum StorageKind { None, String, Double, Integer, ElementId }

/// <summary>
/// A parameter value. Doubles are stored in Revit <b>internal units</b> (feet, ft², ft³, radians);
/// <see cref="Display"/> holds Revit's formatted value string (<c>AsValueString</c>) when available.
/// </summary>
public sealed record ParamValue
{
    public StorageKind Storage { get; init; }
    public bool HasValue { get; init; }
    public string? AsString { get; init; }
    public double? AsDouble { get; init; }
    public long? AsInteger { get; init; }
    public long? AsElementId { get; init; }
    public string? Display { get; init; }

    public static ParamValue Text(string? value) =>
        new() { Storage = StorageKind.String, HasValue = value is not null, AsString = value, Display = value };

    public static ParamValue Number(double value, string? display = null) =>
        new() { Storage = StorageKind.Double, HasValue = true, AsDouble = value, Display = display };

    public static ParamValue Integer(long value, string? display = null) =>
        new() { Storage = StorageKind.Integer, HasValue = true, AsInteger = value, Display = display };

    public static ParamValue Id(long value, string? display = null) =>
        new() { Storage = StorageKind.ElementId, HasValue = value != -1, AsElementId = value, Display = display };

    public static ParamValue Empty(StorageKind storage) => new() { Storage = storage, HasValue = false };
}
