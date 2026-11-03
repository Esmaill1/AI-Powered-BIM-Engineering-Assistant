using RuleCheck.Core.Issues;
using RuleCheck.Core.Rules;

namespace RuleCheck.Engine;

/// <summary>The output of <see cref="RuleEngine.Run"/>.</summary>
public sealed record CheckReport
{
    /// <summary>Model title extracted from the snapshot.</summary>
    public string ModelTitle { get; init; } = "";

    /// <summary>Timestamp when the check run was executed.</summary>
    public DateTimeOffset RunAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>How many rules were evaluated.</summary>
    public int RulesEvaluated { get; init; }

    /// <summary>How many rules were skipped (disabled, not expressible, or target-less).</summary>
    public int RulesSkipped { get; init; }

    /// <summary>How many total elements were scanned.</summary>
    public int ElementsScanned { get; init; }

    /// <summary>All issues found, ordered rule-first.</summary>
    public IReadOnlyList<Issue> Issues { get; init; } = [];

    /// <summary>Rules that failed structurally (bad config, missing field).</summary>
    public IReadOnlyList<RuleError> RuleErrors { get; init; } = [];

    /// <summary>Quick counts per severity.</summary>
    public int ErrorCount => Issues.Count(i => i.Severity == Severity.Error);
    public int WarningCount => Issues.Count(i => i.Severity == Severity.Warning);
    public int InfoCount => Issues.Count(i => i.Severity == Severity.Info);
}

/// <summary>Structural problem with a rule definition (detected before evaluation).</summary>
public sealed record RuleError(string RuleId, string Message);
