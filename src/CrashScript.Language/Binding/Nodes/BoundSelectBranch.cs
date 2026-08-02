using CrashScript.Language.Source;

namespace CrashScript.Language.Binding.Nodes;

public sealed record BoundSelectBranch(
    BoundExpression Condition,
    BoundExpression Value,
    SourceSpan NodeSpan) : BoundNode(NodeSpan);