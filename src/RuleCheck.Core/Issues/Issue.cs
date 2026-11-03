using RuleCheck.Core.Rules;

namespace RuleCheck.Core.Issues;

public enum Severity { Error, Warning, Info }

/// <summary>One rule violation on one element.</summary>
public sealed record Issue(
    string RuleId,
    string RuleName,
    CheckType CheckType,
    Severity Severity,
    long ElementId,
    string Category,
    long? LevelId,
    string Message);
