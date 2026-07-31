using CrashScript.Language.Source;

namespace CrashScript.Language.Lexing;

public sealed record Token(
    TokenType Type,
    string Text,
    object? Value,
    SourceSpan Span)
{
    public override string ToString()
    {
        return $"{Type}: {Text}";
    }
}