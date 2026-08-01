using CrashScript.Language.Diagnostics;

namespace CrashScript.Language.Runtime;

public sealed record ExecutionResult(
    object? LastValue,
    IReadOnlyList<Diagnostic> Diagnostics,
    bool Executed)
{
    public bool Success =>
        Executed &&
        Diagnostics.Count == 0;
}