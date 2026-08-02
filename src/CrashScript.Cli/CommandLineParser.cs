namespace CrashScript.Cli;

public static class CommandLineParser
{
    public static CommandLineParseResult Parse(
        IReadOnlyList<string> arguments,
        bool defaultUseColor)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (arguments.Count == 0)
        {
            return CommandLineParseResult.Succeed(
                new CommandLineOptions(
                    CommandMode.Help,
                    FilePath: null,
                    UseColor: defaultUseColor));
        }

        CommandMode? selectedMode = null;
        string? filePath = null;
        bool useColor = defaultUseColor;

        foreach (string argument in arguments)
        {
            switch (argument)
            {
                case "--no-color":
                    useColor = false;
                    continue;

                case "--help":
                case "-h":
                    if (!TrySelectMode(
                            ref selectedMode,
                            CommandMode.Help,
                            out string? helpError))
                    {
                        return CommandLineParseResult.Fail(
                            helpError!);
                    }

                    continue;

                case "--version":
                case "-v":
                    if (!TrySelectMode(
                            ref selectedMode,
                            CommandMode.Version,
                            out string? versionError))
                    {
                        return CommandLineParseResult.Fail(
                            versionError!);
                    }

                    continue;

                case "--check":
                    if (!TrySelectMode(
                            ref selectedMode,
                            CommandMode.Check,
                            out string? checkError))
                    {
                        return CommandLineParseResult.Fail(
                            checkError!);
                    }

                    continue;

                case "--tokens":
                    if (!TrySelectMode(
                            ref selectedMode,
                            CommandMode.Tokens,
                            out string? tokensError))
                    {
                        return CommandLineParseResult.Fail(
                            tokensError!);
                    }

                    continue;

                case "--ast":
                    if (!TrySelectMode(
                            ref selectedMode,
                            CommandMode.Ast,
                            out string? astError))
                    {
                        return CommandLineParseResult.Fail(
                            astError!);
                    }

                    continue;

                case "--bound":
                    if (!TrySelectMode(
                            ref selectedMode,
                            CommandMode.Bound,
                            out string? boundError))
                    {
                        return CommandLineParseResult.Fail(
                            boundError!);
                    }

                    continue;
            }

            if (argument.Length > 0 &&
                argument[0] == '-')
            {
                return CommandLineParseResult.Fail(
                    $"Unknown option `{argument}`.");
            }

            if (filePath is not null)
            {
                return CommandLineParseResult.Fail(
                    "Only one CrashScript source file can be provided.");
            }

            filePath = argument;
        }

        CommandMode mode =
            selectedMode ?? CommandMode.Execute;

        if (mode is CommandMode.Help or CommandMode.Version)
        {
            if (filePath is not null)
            {
                return CommandLineParseResult.Fail(
                    $"Command `{FormatMode(mode)}` does not accept a source file.");
            }

            return CommandLineParseResult.Succeed(
                new CommandLineOptions(
                    mode,
                    FilePath: null,
                    useColor));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return CommandLineParseResult.Fail(
                $"Command `{FormatMode(mode)}` requires a `.crash` file.");
        }

        return CommandLineParseResult.Succeed(
            new CommandLineOptions(
                mode,
                filePath,
                useColor));
    }

    private static bool TrySelectMode(
        ref CommandMode? selectedMode,
        CommandMode requestedMode,
        out string? error)
    {
        if (selectedMode is not null)
        {
            error =
                $"Command `{FormatMode(requestedMode)}` cannot be combined " +
                $"with `{FormatMode(selectedMode.Value)}`.";

            return false;
        }

        selectedMode = requestedMode;
        error = null;

        return true;
    }

    private static string FormatMode(
        CommandMode mode)
    {
        return mode switch
        {
            CommandMode.Execute => "execute",
            CommandMode.Check => "--check",
            CommandMode.Tokens => "--tokens",
            CommandMode.Ast => "--ast",
            CommandMode.Bound => "--bound",
            CommandMode.Help => "--help",
            CommandMode.Version => "--version",

            _ => mode.ToString()
        };
    }
}