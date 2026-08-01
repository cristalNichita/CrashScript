using System.Globalization;
using CrashScript.Language;
using CrashScript.Language.Binding;
using CrashScript.Language.Diagnostics;
using CrashScript.Language.Lexing;
using CrashScript.Language.Parsing;
using CrashScript.Language.Source;
using CrashScript.Language.Syntax;

namespace CrashScript.Cli;

internal static class Program
{
    private const string Version = "0.1.0";

    private const int SuccessExitCode = 0;
    private const int InvalidArgumentsExitCode = 1;
    private const int FileErrorExitCode = 2;
    private const int LexerErrorExitCode = 3;
    private const int ParserErrorExitCode = 4;
    private const int TypeErrorExitCode = 5;

    public static int Main(string[] args)
    {
        if (args.Length == 1 &&
            IsVersionArgument(args[0]))
        {
            Console.WriteLine(
                $"CrashScript {Version}");

            return SuccessExitCode;
        }

        if (!TryParseArguments(
                args,
                out string filePath,
                out OutputMode outputMode))
        {
            PrintUsage();

            return InvalidArgumentsExitCode;
        }

        try
        {
            var engine = new CrashScriptEngine();
            SourceText source =
                engine.LoadSourceFile(filePath);

            return outputMode switch
            {
                OutputMode.Tokens =>
                    RunLexerMode(engine, source),

                OutputMode.Ast =>
                    RunParserMode(engine, source),

                OutputMode.Bound =>
                    RunBindingMode(
                        engine,
                        source,
                        printTree: true),

                _ =>
                    RunBindingMode(
                        engine,
                        source,
                        printTree: false)
            };
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
            PrintFileError(
                $"Access denied: {exception.Message}");

            return FileErrorExitCode;
        }
        catch (IOException exception)
        {
            PrintFileError(
                $"Could not read the source file: {exception.Message}");

            return FileErrorExitCode;
        }
    }

    private static int RunLexerMode(
        CrashScriptEngine engine,
        SourceText source)
    {
        LexResult result =
            engine.Tokenize(source);

        PrintTokens(result.Tokens);

        if (result.Diagnostics.Count > 0)
        {
            PrintDiagnostics(result.Diagnostics);

            return LexerErrorExitCode;
        }

        return SuccessExitCode;
    }

    private static int RunParserMode(
        CrashScriptEngine engine,
        SourceText source)
    {
        ParseResult result =
            engine.Parse(source);

        if (result.Diagnostics.Count > 0)
        {
            PrintDiagnostics(result.Diagnostics);

            return GetDiagnosticExitCode(
                result.Diagnostics);
        }

        Console.WriteLine(
            $"CrashScript {Version}");
        Console.WriteLine();
        Console.WriteLine("SYNTAX AST");
        Console.WriteLine(
            "----------------------------------------");
        Console.WriteLine(
            SyntaxTreePrinter.Print(result.Root));
        Console.WriteLine(
            "----------------------------------------");

        return SuccessExitCode;
    }

    private static int RunBindingMode(
        CrashScriptEngine engine,
        SourceText source,
        bool printTree)
    {
        BindResult result =
            engine.Bind(source);

        if (result.Diagnostics.Count > 0)
        {
            PrintDiagnostics(result.Diagnostics);

            return GetDiagnosticExitCode(
                result.Diagnostics);
        }

        if (printTree)
        {
            Console.WriteLine(
                $"CrashScript {Version}");
            Console.WriteLine();
            Console.WriteLine("BOUND AST");
            Console.WriteLine(
                "----------------------------------------");
            Console.WriteLine(
                BoundTreePrinter.Print(result.Root));
            Console.WriteLine(
                "----------------------------------------");

            return SuccessExitCode;
        }

        Console.WriteLine(
            $"CrashScript {Version}");
        Console.WriteLine(
            $"Type-checked: {source.FileName}");
        Console.WriteLine(
            $"Statements: {result.Root.Statements.Count}");
        Console.WriteLine("Lexer errors: 0");
        Console.WriteLine("Parser errors: 0");
        Console.WriteLine("Type errors: 0");
        Console.WriteLine();
        Console.WriteLine(
            "The source is valid at the type level.");
        Console.WriteLine(
            "Interpreter is not implemented yet.");

        return SuccessExitCode;
    }

    private static int GetDiagnosticExitCode(
        IReadOnlyList<Diagnostic> diagnostics)
    {
        if (diagnostics.Any(diagnostic =>
                diagnostic.Category ==
                DiagnosticCategory.Lexer))
        {
            return LexerErrorExitCode;
        }

        if (diagnostics.Any(diagnostic =>
                diagnostic.Category ==
                DiagnosticCategory.Parser))
        {
            return ParserErrorExitCode;
        }

        return TypeErrorExitCode;
    }

    private static bool TryParseArguments(
        string[] args,
        out string filePath,
        out OutputMode outputMode)
    {
        filePath = string.Empty;
        outputMode = OutputMode.Validate;

        if (args.Length == 1)
        {
            filePath = args[0];
            return true;
        }

        if (args.Length != 2)
        {
            return false;
        }

        outputMode = args[0] switch
        {
            "--tokens" => OutputMode.Tokens,
            "--ast" => OutputMode.Ast,
            "--bound" => OutputMode.Bound,
            _ => OutputMode.Invalid
        };

        if (outputMode == OutputMode.Invalid)
        {
            return false;
        }

        filePath = args[1];

        return true;
    }

    private static bool IsVersionArgument(
        string argument)
    {
        return argument is "--version" or "-v";
    }

    private static void PrintTokens(
        IReadOnlyList<Token> tokens)
    {
        Console.WriteLine(
            $"CrashScript {Version}");
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
            SourceLocation location =
                token.Span.StartLocation;

            string locationText =
                $"{location.Line}:{location.Column}";

            string tokenText =
                token.Type == TokenType.EndOfFile
                    ? "<eof>"
                    : $"\"{Escape(token.Text)}\"";

            string valueText =
                FormatValue(token.Value);

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
            $"CrashScript found {diagnostics.Count} error(s).");
        Console.Error.WriteLine();

        for (int index = 0;
             index < diagnostics.Count;
             index++)
        {
            Console.Error.WriteLine(
                DiagnosticRenderer.Render(
                    diagnostics[index]));

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
            bool boolean =>
                boolean ? "true" : "false",

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
        Console.WriteLine(
            $"CrashScript {Version}");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine(
            "  crashscript <file.crash>");
        Console.WriteLine(
            "  crashscript --tokens <file.crash>");
        Console.WriteLine(
            "  crashscript --ast <file.crash>");
        Console.WriteLine(
            "  crashscript --bound <file.crash>");
        Console.WriteLine(
            "  crashscript --version");
    }

    private static void PrintFileError(
        string message)
    {
        Console.Error.WriteLine(
            "CrashScript could not start.");
        Console.Error.WriteLine(message);
    }

    private enum OutputMode
    {
        Invalid,
        Validate,
        Tokens,
        Ast,
        Bound
    }
}