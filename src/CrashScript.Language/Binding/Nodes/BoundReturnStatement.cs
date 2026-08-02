using CrashScript.Language.Source;

namespace CrashScript.Language.Binding.Nodes;

public sealed record BoundReturnStatement(
    BoundExpression? Expression,
    SourceSpan NodeSpan) : BoundStatement(NodeSpan);