using CrashScript.Language.Diagnostics;
using CrashScript.Language.Syntax;

namespace CrashScript.Language.Parsing;

public sealed record ParseResult(
    CompilationUnitSyntax Root,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    public bool Success => Diagnostics.Count == 0;
}