using CrashScript.Language.Source;

namespace CrashScript.Language.Binding.Nodes;

public abstract record BoundStatement(
    SourceSpan Span) : BoundNode(Span);