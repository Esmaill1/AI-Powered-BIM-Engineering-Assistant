namespace RuleCheck.Core.Rules;

/// <summary>A named, reusable collection of rules (saved/loaded as JSON).</summary>
public sealed record RuleSet
{
    public string SchemaVersion { get; init; } = RuleDefinition.CurrentSchemaVersion;
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public IReadOnlyList<RuleDefinition> Rules { get; init; } = [];
}
