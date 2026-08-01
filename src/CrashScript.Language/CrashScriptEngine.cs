using CrashScript.Language.Diagnostics;
using CrashScript.Language.Lexing;
using CrashScript.Language.Parsing;
using CrashScript.Language.Source;

namespace CrashScript.Language;

/// <summary>
/// Main entry point for the CrashScript compilation pipeline.
/// </summary>
public sealed class CrashScriptEngine
{
    public const string FileExtension = ".crash";

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

    public ParseResult Parse(SourceText source)
    {
        ArgumentNullException.ThrowIfNull(source);

        LexResult lexResult = Tokenize(source);

        var parser = new Parser(lexResult.Tokens);
        ParseResult parseResult = parser.Parse();

        Diagnostic[] diagnostics = lexResult.Diagnostics
            .Concat(parseResult.Diagnostics)
            .OrderBy(diagnostic => diagnostic.Span.Start)
            .ToArray();

        return new ParseResult(
            parseResult.Root,
            diagnostics);
    }

    public ParseResult ParseFile(string filePath)
    {
        SourceText source = LoadSourceFile(filePath);

        return Parse(source);
    }
}