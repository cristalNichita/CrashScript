using CrashScript.Language.Source;

namespace CrashScript.Language.Binding.Nodes;

public sealed record BoundExpressionStatement(
    BoundExpression Expression,
    SourceSpan Span) : BoundStatement(Span);