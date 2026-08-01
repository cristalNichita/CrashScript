using CrashScript.Language.Source;

namespace CrashScript.Language.Binding.Nodes;

public sealed record BoundWhileStatement(
    BoundExpression Condition,
    BoundBlockStatement Body,
    SourceSpan Span) : BoundStatement(Span);