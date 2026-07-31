using System.Collections;
using CrashScript.Language.Source;

namespace CrashScript.Language.Diagnostics;

/// <summary>
/// Mutable diagnostic collection used while processing source code.
/// </summary>
public sealed class DiagnosticBag : IReadOnlyCollection<Diagnostic>
{
    private readonly List<Diagnostic> _diagnostics = [];

    public int Count => _diagnostics.Count;
    
    public bool HasErrors => _diagnostics.Count > 0;

    public void Report(
        string code,
        DiagnosticCategory category,
        string message,
        SourceSpan span,
        string? hint = null,
        string? joke = null)
    {
        _diagnostics.Add(
            new Diagnostic(
                code,
                category,
                message,
                span,
                hint,
                joke));
    }

    public Diagnostic[] ToArray()
    {
        return _diagnostics.ToArray();
    }

    public IEnumerator<Diagnostic> GetEnumerator()
    {
        return _diagnostics.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}