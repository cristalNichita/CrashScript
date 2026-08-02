namespace CrashScript.Cli;

public sealed record CommandLineParseResult(
    CommandLineOptions? Options,
    string? Error)
{
    public bool Success =>
        Options is not null;

    public static CommandLineParseResult Succeed(
        CommandLineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new CommandLineParseResult(
            options,
            Error: null);
    }

    public static CommandLineParseResult Fail(
        string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        return new CommandLineParseResult(
            Options: null,
            error);
    }
}