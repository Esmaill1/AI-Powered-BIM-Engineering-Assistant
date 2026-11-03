using RuleCheck.Core.Issues;
using RuleCheck.Core.Rules;
using RuleCheck.Core.Snapshot;

namespace RuleCheck.Engine;

/// <summary>Evaluates one <see cref="CheckType"/> against the already-filtered target elements.</summary>
public interface ICheckEvaluator
{
    CheckType Type { get; }

    IEnumerable<Issue> Evaluate(RuleDefinition rule, IReadOnlyList<ElementSnapshot> targets, EvaluationContext context);
}

/// <summary>Thrown when a rule is structurally incomplete for its check type (e.g. <c>pattern</c> without <c>regex</c>).</summary>
public sealed class RuleConfigurationException(string message) : Exception(message);
