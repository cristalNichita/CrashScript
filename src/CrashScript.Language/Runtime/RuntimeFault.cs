using CrashScript.Language.Diagnostics;
using CrashScript.Language.Source;

namespace CrashScript.Language.Runtime;

/// <summary>
/// An expected user-facing failure that occurs while executing
/// an otherwise valid CrashScript program.
/// </summary>
public sealed class RuntimeFault : Exception
{
    public RuntimeFault(
        string code,
        string message,
        SourceSpan span,
        string? hint = null,
        string? joke = null)
        : base(message)
    {
        Code = code;
        Span = span;
        Hint = hint;
        Joke = joke;
    }

    public string Code { get; }

    public SourceSpan Span { get; }

    public string? Hint { get; }

    public string? Joke { get; }

    public Diagnostic ToDiagnostic()
    {
        return new Diagnostic(
            Code,
            DiagnosticCategory.Runtime,
            Message,
            Span,
            Hint,
            Joke);
    }
}