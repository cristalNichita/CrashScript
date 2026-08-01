using CrashScript.Language.Source;

namespace CrashScript.Language.Binding.Nodes;

public sealed record BoundIfStatement(
    IReadOnlyList<BoundIfBranch> Branches,
    BoundBlockStatement? ElseBody,
    SourceSpan Span) : BoundStatement(Span);