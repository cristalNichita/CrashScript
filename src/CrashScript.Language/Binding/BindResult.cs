using CrashScript.Language.Binding.Nodes;
using CrashScript.Language.Diagnostics;

namespace CrashScript.Language.Binding;

public sealed record BindResult(
    BoundCompilationUnit Root,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    public bool Success => Diagnostics.Count == 0;
}