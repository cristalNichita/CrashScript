using CrashScript.Language.Lexing;
using CrashScript.Language.Source;

namespace CrashScript.Language;

/// <summary>
///     Main entry point for the CrashScript compilation pipeline.
/// </summary>
public sealed class CrashScriptEngine
{
    public const string FileExtension = ".crash";

    /// <summary>
    ///     Loads and validates a CrashScript source file.
    /// </summary>
    public SourceText LoadSourceFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var fullPath = Path.GetFullPath(filePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException(
                $"CrashScript source file was not found: {fullPath}",
                fullPath);

        string extension = Path.GetExtension(fullPath);

        if (!string.Equals(
                extension,
                FileExtension,
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(
                $"Expected a '{FileExtension}' file, but received '{extension}'.");

        var sourceCode = File.ReadAllText(fullPath);

        return new SourceText(fullPath, sourceCode);
    }

    public LexResult Tokenize(SourceText source)
    {
        ArgumentNullException.ThrowIfNull(source);
        
        var lexer = new Lexer(source);
        
        return lexer.Lex();
    }

    public LexResult TokenizeFile(string filePath)
    {
        SourceText source = LoadSourceFile(filePath);

        return Tokenize(source);
    }
}