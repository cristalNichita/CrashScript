using CrashScript.Language.Diagnostics;

namespace CrashScript.Language.Lexing;

public sealed record LexResult(
    IReadOnlyList<Token> Tokens,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    public bool Success => Diagnostics.Count == 0;
}