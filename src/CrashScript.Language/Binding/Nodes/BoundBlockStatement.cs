using CrashScript.Language.Source;

namespace CrashScript.Language.Binding.Nodes;

public sealed record BoundBlockStatement(
    IReadOnlyList<BoundStatement> Statements,
    SourceSpan Span) : BoundStatement(Span);