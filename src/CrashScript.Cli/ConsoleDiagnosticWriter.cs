using CrashScript.Language.Diagnostics;

namespace CrashScript.Cli;

public static class ConsoleDiagnosticWriter
{
    public static void Write(
        IReadOnlyList<Diagnostic> diagnostics,
        bool useColor)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        WriteColoredLine(
            $"CrashScript found {diagnostics.Count} error(s).",
            ConsoleColor.Red,
            useColor);

        Console.Error.WriteLine();

        for (int index = 0;
             index < diagnostics.Count;
             index++)
        {
            WriteDiagnostic(
                diagnostics[index],
                useColor);

            if (index + 1 < diagnostics.Count)
            {
                Console.Error.WriteLine();
            }
        }
    }

    public static void WriteSimpleError(
        string title,
        string message,
        bool useColor)
    {
        WriteColoredLine(
            title,
            ConsoleColor.Red,
            useColor);

        Console.Error.WriteLine(message);
    }

    private static void WriteDiagnostic(
        Diagnostic diagnostic,
        bool useColor)
    {
        string rendered =
            DiagnosticRenderer.Render(diagnostic);

        using var reader =
            new StringReader(rendered);

        while (reader.ReadLine() is { } line)
        {
            if (line.StartsWith(
                    "CRASH-",
                    StringComparison.Ordinal))
            {
                WriteColoredLine(
                    line,
                    ConsoleColor.Red,
                    useColor);

                continue;
            }

            if (line.StartsWith(
                    "Hint:",
                    StringComparison.Ordinal))
            {
                WriteColoredLine(
                    line,
                    ConsoleColor.Yellow,
                    useColor);

                continue;
            }

            if (!string.IsNullOrWhiteSpace(
                    diagnostic.Joke) &&
                string.Equals(
                    line,
                    diagnostic.Joke,
                    StringComparison.Ordinal))
            {
                WriteColoredLine(
                    line,
                    ConsoleColor.DarkGray,
                    useColor);

                continue;
            }

            Console.Error.WriteLine(line);
        }
    }

    private static void WriteColoredLine(
        string text,
        ConsoleColor color,
        bool useColor)
    {
        if (!useColor)
        {
            Console.Error.WriteLine(text);
            return;
        }

        ConsoleColor previousColor =
            Console.ForegroundColor;

        try
        {
            Console.ForegroundColor = color;
            Console.Error.WriteLine(text);
        }
        finally
        {
            Console.ForegroundColor =
                previousColor;
        }
    }
}