using System.Globalization;
using CrashScript.Language;
using CrashScript.Language.Binding;
using CrashScript.Language.Diagnostics;
using CrashScript.Language.Lexing;
using CrashScript.Language.Parsing;
using CrashScript.Language.Runtime;
using CrashScript.Language.Source;
using CrashScript.Language.Syntax;

namespace CrashScript.Cli;

internal static class Program
{
    public static int Main(string[] args)
    {
        bool defaultUseColor =
            !Console.IsOutputRedirected &&
            !Console.IsErrorRedirected;

        CommandLineParseResult parseResult =
            CommandLineParser.Parse(
                args,
                defaultUseColor);

        if (parseResult.Options is not { } options)
        {
            ConsoleDiagnosticWriter.WriteSimpleError(
                "Invalid command-line arguments.",
                parseResult.Error ??
                "The command could not be parsed.",
                defaultUseColor);

            Console.Error.WriteLine();
            Console.Error.WriteLine(
                "Run `crashscript --help` to see available commands.");

            return ExitCodes.InvalidArguments;
        }

        if (options.Mode == CommandMode.Help)
        {
            HelpText.Write();
            return ExitCodes.Success;
        }

        if (options.Mode == CommandMode.Version)
        {
            Console.WriteLine(
                $"{AppInfo.Name} {AppInfo.Version}");

            return ExitCodes.Success;
        }

        string filePath =
            options.FilePath ??
            throw new InvalidOperationException(
                "A file command was created without a file path.");

        try
        {
            var engine =
                new CrashScriptEngine();

            SourceText source =
                engine.LoadSourceFile(filePath);

            return options.Mode switch
            {
                CommandMode.Execute =>
                    RunExecutionMode(
                        engine,
                        source,
                        options.UseColor),

                CommandMode.Check =>
                    RunCheckMode(
                        engine,
                        source,
                        options.UseColor),

                CommandMode.Tokens =>
                    RunLexerMode(
                        engine,
                        source,
                        options.UseColor),

                CommandMode.Ast =>
                    RunParserMode(
                        engine,
                        source,
                        options.UseColor),

                CommandMode.Bound =>
                    RunBoundMode(
                        engine,
                        source,
                        options.UseColor),

                _ =>
                    throw new InvalidOperationException(
                        $"Unsupported command mode `{options.Mode}`.")
            };
        }
        catch (FileNotFoundException exception)
        {
            ConsoleDiagnosticWriter.WriteSimpleError(
                "CrashScript source file was not found.",
                exception.FileName ??
                exception.Message,
                options.UseColor);

            return ExitCodes.FileError;
        }
        catch (InvalidDataException exception)
        {
            ConsoleDiagnosticWriter.WriteSimpleError(
                "CrashScript could not open the source file.",
                exception.Message,
                options.UseColor);

            return ExitCodes.FileError;
        }
        catch (UnauthorizedAccessException exception)
        {
            ConsoleDiagnosticWriter.WriteSimpleError(
                "Access to the source file was denied.",
                exception.Message,
                options.UseColor);

            return ExitCodes.FileError;
        }
        catch (IOException exception)
        {
            ConsoleDiagnosticWriter.WriteSimpleError(
                "CrashScript could not read the source file.",
                exception.Message,
                options.UseColor);

            return ExitCodes.FileError;
        }
        catch (Exception exception)
        {
            PrintInternalError(
                exception,
                options.UseColor);

            return ExitCodes.InternalError;
        }
    }

    private static int RunExecutionMode(
        CrashScriptEngine engine,
        SourceText source,
        bool useColor)
    {
        ExecutionResult result =
            engine.Execute(source);

        if (result.Diagnostics.Count == 0)
        {
            return ExitCodes.Success;
        }

        ConsoleDiagnosticWriter.Write(
            result.Diagnostics,
            useColor);

        return GetDiagnosticExitCode(
            result.Diagnostics);
    }

    private static int RunCheckMode(
        CrashScriptEngine engine,
        SourceText source,
        bool useColor)
    {
        BindResult result =
            engine.Bind(source);

        if (result.Diagnostics.Count > 0)
        {
            ConsoleDiagnosticWriter.Write(
                result.Diagnostics,
                useColor);

            return GetDiagnosticExitCode(
                result.Diagnostics);
        }

        WriteSuccessLine(
            $"Checked `{source.FileName}` successfully.",
            useColor);

        Console.WriteLine(
            $"Statements: {result.Root.Statements.Count}");

        Console.WriteLine(
            "Lexer errors: 0");

        Console.WriteLine(
            "Parser errors: 0");

        Console.WriteLine(
            "Type errors: 0");

        return ExitCodes.Success;
    }

    private static int RunLexerMode(
        CrashScriptEngine engine,
        SourceText source,
        bool useColor)
    {
        LexResult result =
            engine.Tokenize(source);

        PrintTokens(result.Tokens);

        if (result.Diagnostics.Count == 0)
        {
            return ExitCodes.Success;
        }

        ConsoleDiagnosticWriter.Write(
            result.Diagnostics,
            useColor);

        return ExitCodes.LexerError;
    }

    private static int RunParserMode(
        CrashScriptEngine engine,
        SourceText source,
        bool useColor)
    {
        ParseResult result =
            engine.Parse(source);

        if (result.Diagnostics.Count > 0)
        {
            ConsoleDiagnosticWriter.Write(
                result.Diagnostics,
                useColor);

            return GetDiagnosticExitCode(
                result.Diagnostics);
        }

        Console.WriteLine(
            $"{AppInfo.Name} {AppInfo.Version}");

        Console.WriteLine();
        Console.WriteLine("SYNTAX AST");
        Console.WriteLine(
            "----------------------------------------");

        Console.WriteLine(
            SyntaxTreePrinter.Print(
                result.Root));

        Console.WriteLine(
            "----------------------------------------");

        return ExitCodes.Success;
    }

    private static int RunBoundMode(
        CrashScriptEngine engine,
        SourceText source,
        bool useColor)
    {
        BindResult result =
            engine.Bind(source);

        if (result.Diagnostics.Count > 0)
        {
            ConsoleDiagnosticWriter.Write(
                result.Diagnostics,
                useColor);

            return GetDiagnosticExitCode(
                result.Diagnostics);
        }

        Console.WriteLine(
            $"{AppInfo.Name} {AppInfo.Version}");

        Console.WriteLine();
        Console.WriteLine("BOUND AST");
        Console.WriteLine(
            "----------------------------------------");

        Console.WriteLine(
            BoundTreePrinter.Print(
                result.Root));

        Console.WriteLine(
            "----------------------------------------");

        return ExitCodes.Success;
    }

    private static int GetDiagnosticExitCode(
        IReadOnlyList<Diagnostic> diagnostics)
    {
        if (diagnostics.Any(diagnostic =>
                diagnostic.Category ==
                DiagnosticCategory.Lexer))
        {
            return ExitCodes.LexerError;
        }

        if (diagnostics.Any(diagnostic =>
                diagnostic.Category ==
                DiagnosticCategory.Parser))
        {
            return ExitCodes.ParserError;
        }

        if (diagnostics.Any(diagnostic =>
                diagnostic.Category ==
                DiagnosticCategory.Type))
        {
            return ExitCodes.TypeError;
        }

        return ExitCodes.RuntimeError;
    }

    private static void PrintTokens(
        IReadOnlyList<Token> tokens)
    {
        Console.WriteLine(
            $"{AppInfo.Name} {AppInfo.Version}");

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
                $" {locationText,-9} " +
                $"{token.Type,-24} " +
                $"{tokenText,-24} " +
                $"{valueText}");
        }

        Console.WriteLine(
            "--------------------------------------------------------------------------");
    }

    private static string FormatValue(
        object? value)
    {
        return value switch
        {
            null => "-",

            string text =>
                $"\"{Escape(text)}\"",

            bool boolean =>
                boolean ? "true" : "false",

            IFormattable formattable =>
                formattable.ToString(
                    null,
                    CultureInfo.InvariantCulture),

            _ => value.ToString() ?? "-"
        };
    }

    private static string Escape(
        string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t")
            .Replace("\"", "\\\"");
    }

    private static void WriteSuccessLine(
        string text,
        bool useColor)
    {
        if (!useColor)
        {
            Console.WriteLine(text);
            return;
        }

        ConsoleColor previousColor =
            Console.ForegroundColor;

        try
        {
            Console.ForegroundColor =
                ConsoleColor.Green;

            Console.WriteLine(text);
        }
        finally
        {
            Console.ForegroundColor =
                previousColor;
        }
    }

    private static void PrintInternalError(
        Exception exception,
        bool useColor)
    {
        ConsoleDiagnosticWriter.WriteSimpleError(
            "CRASH-INTERNAL: The CrashScript interpreter failed unexpectedly.",
            exception.Message,
            useColor);

        bool debugEnabled =
            string.Equals(
                Environment.GetEnvironmentVariable(
                    "CRASHSCRIPT_DEBUG"),
                "1",
                StringComparison.Ordinal);

        if (debugEnabled)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine(exception);
            return;
        }

        Console.Error.WriteLine();
        Console.Error.WriteLine(
            "Set CRASHSCRIPT_DEBUG=1 to print the internal stack trace.");
    }
}