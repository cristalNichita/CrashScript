namespace CrashScript.Language.Source;

/// <summary>
///     Represents a single position inside a source file.
///     Line and column numbers are one-based for user-facing diagnostics.
/// </summary>
public readonly record struct SourceLocation(
    int Offset,
    int Line,
    int Column)
{
    public override string ToString()
    {
        return $"{Line}:{Column}";
    }
}