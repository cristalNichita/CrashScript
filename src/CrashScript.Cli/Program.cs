using System.Globalization;
using CrashScript.Language;
using CrashScript.Language.Diagnostics;
using CrashScript.Language.Lexing;
using CrashScript.Language.Source;

namespace CrashScript.Cli;

internal static class Program
{
    private const string Version = "0.1.0";

    private const int SuccessExitCode = 0;
    private const int InvalidArgumentsExitCode = 1;
    private const int FileErrorExitCode = 2;
    private const int LexerErrorExitCode = 3;

    public static int Main(string[] args)
    {
        if (args.Length == 1 && IsVersionArgument(args[0]))
        {
            Console.WriteLine($"CrashScript {Version}");
            return SuccessExitCode;
        }

        if (!TryParseArguments(
                args,
                out string? filePath,
                out bool printTokens))
        {
            PrintUsage();
            return InvalidArgumentsExitCode;
        }

        try
        {
            var engine = new CrashScriptEngine();

            SourceText source = engine.LoadSourceFile(filePath);
            LexResult result = engine.Tokenize(source);

            if (printTokens)
            {
                PrintTokens(result.Tokens);
            }

            if (result.Diagnostics.Count > 0)
            {
                PrintDiagnostics(result.Diagnostics);
                return LexerErrorExitCode;
            }

            if (!printTokens)
            {
                PrintSuccess(source, result);
            }

            return SuccessExitCode;
        }
        catch (FileNotFoundException exception)
        {
            PrintFileError(exception.Message);
            return FileErrorExitCode;
        }
        catch (InvalidDataException exception)
        {
            PrintFileError(exception.Message);
            return FileErrorExitCode;
        }
        catch (UnauthorizedAccessException exception)
        {
            PrintFileError($"Access denied: {exception.Message}");
            return FileErrorExitCode;
        }
        catch (IOException exception)
        {
            PrintFileError(
                $"Could not read the source file: {exception.Message}");

            return FileErrorExitCode;
        }
    }

    private static bool TryParseArguments(
        string[] args,
        out string filePath,
        out bool printTokens)
    {
        filePath = string.Empty;
        printTokens = false;

        if (args.Length == 1)
        {
            filePath = args[0];
            return true;
        }

        if (args.Length == 2 && args[0] == "--tokens")
        {
            printTokens = true;
            filePath = args[1];
            return true;
        }

        return false;
    }

    private static bool IsVersionArgument(string argument)
    {
        return argument is "--version" or "-v";
    }

    private static void PrintSuccess(
        SourceText source,
        LexResult result)
    {
        int tokenCount = result.Tokens.Count(token =>
            token.Type != TokenType.EndOfFile);

        Console.WriteLine($"CrashScript {Version}");
        Console.WriteLine($"Lexed: {source.FileName}");
        Console.WriteLine($"Tokens: {tokenCount}");
        Console.WriteLine("Lexer errors: 0");
        Console.WriteLine();
        Console.WriteLine(
            "The source is valid at the lexical level.");
        Console.WriteLine(
            "Parser and interpreter are not implemented yet.");
    }

    private static void PrintTokens(IReadOnlyList<Token> tokens)
    {
        Console.WriteLine($"CrashScript {Version}");
        Console.WriteLine();
        Console.WriteLine("TOKENS");
        Console.WriteLine(
            "--------------------------------------------------------------------------");
        Console.WriteLine(
            " LOCATION  TYPE                     TEXT                     VALUE");
        Console.WriteLine(
            "--------------------------------------------------------------------------");

        foreach (Token token in tokens)
        {
            SourceLocation location = token.Span.StartLocation;

            string locationText =
                $"{location.Line}:{location.Column}";

            string tokenText = token.Type == TokenType.EndOfFile
                ? "<eof>"
                : $"\"{Escape(token.Text)}\"";

            string valueText = FormatValue(token.Value);

            Console.WriteLine(
                $" {locationText,-9} {token.Type,-24} {tokenText,-24} {valueText}");
        }

        Console.WriteLine(
            "--------------------------------------------------------------------------");
        Console.WriteLine();
    }

    private static void PrintDiagnostics(
        IReadOnlyList<Diagnostic> diagnostics)
    {
        Console.Error.WriteLine(
            $"CrashScript found {diagnostics.Count} lexer error(s).");
        Console.Error.WriteLine();

        for (int index = 0; index < diagnostics.Count; index++)
        {
            Console.Error.WriteLine(
                DiagnosticRenderer.Render(diagnostics[index]));

            if (index + 1 < diagnostics.Count)
            {
                Console.Error.WriteLine();
            }
        }
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "-",
            string text => $"\"{Escape(text)}\"",
            bool boolean => boolean ? "true" : "false",
            IFormattable formattable =>
                formattable.ToString(
                    null,
                    CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "-"
        };
    }

    private static string Escape(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t")
            .Replace("\"", "\\\"");
    }

    private static void PrintUsage()
    {
        Console.WriteLine($"CrashScript {Version}");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  crashscript <file.crash>");
        Console.WriteLine("  crashscript --tokens <file.crash>");
        Console.WriteLine("  crashscript --version");
        Console.WriteLine();
        Console.WriteLine("Development:");
        Console.WriteLine(
            "  dotnet run --project src/CrashScript.Cli -- examples/hello.crash");
        Console.WriteLine(
            "  dotnet run --project src/CrashScript.Cli -- --tokens examples/hello.crash");
    }

    private static void PrintFileError(string message)
    {
        Console.Error.WriteLine("CrashScript could not start.");
        Console.Error.WriteLine(message);
    }
}