using System.Text.Json.Serialization;
using RuleCheck.Core.Issues;

namespace RuleCheck.Core.Rules;

/// <summary>
/// In-memory form of a rule. JSON shape is defined by <c>schemas/rule.schema.json</c> (v1);
/// serialize with <see cref="Json.RuleCheckJson.Options"/>.
/// </summary>
public sealed record RuleDefinition
{
    public const string CurrentSchemaVersion = "1.0";

    public string SchemaVersion { get; init; } = CurrentSchemaVersion;
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string SourceQuote { get; init; } = "";
    public Severity Severity { get; init; } = Severity.Error;
    public bool IsExpressible { get; init; } = true;
    public string? UnexpressibleReason { get; init; }
    public IReadOnlyList<string>? UnresolvedParameters { get; init; }
    public RuleTarget? Target { get; init; }
    public CheckSpec? Check { get; init; }

    /// <summary>Review status, derived from the stored fields (never serialized).</summary>
    [JsonIgnore]
    public RuleStatus Status =>
        !IsExpressible ? RuleStatus.NotExpressible
        : UnresolvedParameters is { Count: > 0 } ? RuleStatus.Unresolved
        : RuleStatus.Ok;
}

public enum RuleStatus { Ok, Unresolved, NotExpressible }

public sealed record RuleTarget
{
    public required IReadOnlyList<string> Categories { get; init; }
    public ElementKind ElementKind { get; init; } = ElementKind.Instance;
    public IReadOnlyList<WhereCondition>? Where { get; init; }
}

public enum ElementKind { Instance, Type, Both }

public sealed record WhereCondition
{
    public required string Parameter { get; init; }
    public required ConditionOperator Operator { get; init; }
    public required RuleValue Value { get; init; }
    public string? Unit { get; init; }
}

public enum ConditionOperator { Equals, NotEquals, Contains, In, GreaterThan, LessThan }

/// <summary>The single deterministic check of a rule. Which fields are required depends on <see cref="Type"/>.</summary>
public sealed record CheckSpec
{
    public required CheckType Type { get; init; }
    public string? Parameter { get; init; }
    public string? Regex { get; init; }
    public IReadOnlyList<string>? Values { get; init; }
    public bool? CaseSensitive { get; init; }
    public double? Min { get; init; }
    public double? Max { get; init; }
    public string? Unit { get; init; }
    public UniqueScope? Scope { get; init; }
    public PlacementKind? PlacementKind { get; init; }
}

public enum CheckType { RequiredParameter, Pattern, AllowedValues, Range, Unique, Placement, NotUnused }

public enum UniqueScope { Model, Category, Level }

public enum PlacementKind { RoomAssigned, LevelAssigned }
