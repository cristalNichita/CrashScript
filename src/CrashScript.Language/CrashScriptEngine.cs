using CrashScript.Language.Source;

namespace CrashScript.Language;


/// <summary>
/// Main entry point for working with CrashScript source code.
/// The lexer, parser, binder and interpreter will later be connected here.
/// </summary>
public sealed class CrashScriptEngine
{
    public const string FileExtension = ".crash";

    /// <summary>
    /// Loads and validates a CrashScript source file.
    /// </summary>
    public SourceText LoadSourceFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string fullPath = Path.GetFullPath(filePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"CrashScript source file was not found: {fullPath}",
                fullPath);
        }

        string extension = Path.GetExtension(fullPath);

        if (!string.Equals(
                extension,
                FileExtension,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Expected a '{FileExtension}' file, but received '{extension}'.");
        }

        string sourceCode = File.ReadAllText(fullPath);

        return new SourceText(fullPath, sourceCode);
    }
}