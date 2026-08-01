using CrashScript.Language.Source;

namespace CrashScript.Language.Binding.Nodes;

public sealed record BoundCompilationUnit(
    IReadOnlyList<BoundStatement> Statements,
    SourceSpan Span) : BoundNode(Span);