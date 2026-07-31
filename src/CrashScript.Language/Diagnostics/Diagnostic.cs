using CrashScript.Language.Source;

namespace CrashScript.Language.Diagnostics;

public sealed record Diagnostic(
    string Code,
    DiagnosticCategory Category,
    string Message,
    SourceSpan Span,
    string? Hint = null,
    string? Joke = null);