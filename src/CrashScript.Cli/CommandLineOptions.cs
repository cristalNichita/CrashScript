namespace CrashScript.Cli;

public sealed record CommandLineOptions(
    CommandMode Mode,
    string? FilePath,
    bool UseColor);