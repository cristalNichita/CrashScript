using CrashScript.Language.Source;

namespace CrashScript.Language.Binding.Nodes;

public sealed record BoundIfBranch(
    BoundExpression Condition,
    BoundBlockStatement Body,
    SourceSpan Span) : BoundNode(Span);