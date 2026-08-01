using CrashScript.Language.Source;

namespace CrashScript.Language.Binding.Nodes;

public abstract record BoundNode(
    SourceSpan Span);