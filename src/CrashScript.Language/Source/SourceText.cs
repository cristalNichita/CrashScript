namespace CrashScript.Language.Source;

/// <summary>
/// Represents the complete source code of a CrashScript file.
/// Later this class will also store line boundaries for diagnostics.
/// </summary>
public sealed class SourceText
{
    public SourceText(string filePath, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(text);
        
        FilePath = Path.GetFullPath(filePath);
        Text = text;
    }
    
    /// <summary>
    /// Absolute path to the source file.
    /// </summary>
    public string FilePath { get; }
    
    /// <summary>
    /// File name without its directory.
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);
    
    /// <summary>
    /// Complete source code.
    /// </summary>
    public string Text { get; }
    
    /// <summary>
    /// Number of UTF-16 characters in the source.
    /// </summary>
    public int Length => Text.Length; 
}