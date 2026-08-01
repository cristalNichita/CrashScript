using CrashScript.Language.Source;

namespace CrashScript.Language.Syntax;

public abstract record SyntaxNode
{
    public abstract SourceSpan Span { get; }
}