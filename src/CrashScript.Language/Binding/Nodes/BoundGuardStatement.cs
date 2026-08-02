using CrashScript.Language.Source;

namespace CrashScript.Language.Binding.Nodes;

public sealed record BoundGuardStatement(
    BoundExpression Condition,
    BoundExpression Message,
    SourceSpan NodeSpan) : BoundStatement(NodeSpan);