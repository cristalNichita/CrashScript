using CrashScript.Cli;
using Xunit;

namespace CrashScript.Tests;

public sealed class CommandLineParserTests
{
    [Fact]
    public void Parse_NoArgumentsShowsHelp()
    {
        CommandLineOptions options =
            ParseSuccessfully();

        Assert.Equal(
            CommandMode.Help,
            options.Mode);

        Assert.Null(options.FilePath);
    }

    [Fact]
    public void Parse_VersionFlagSelectsVersionMode()
    {
        CommandLineOptions options =
            ParseSuccessfully("--version");

        Assert.Equal(
            CommandMode.Version,
            options.Mode);
    }

    [Fact]
    public void Parse_FileWithoutCommandExecutesProgram()
    {
        CommandLineOptions options =
            ParseSuccessfully("game.crash");

        Assert.Equal(
            CommandMode.Execute,
            options.Mode);

        Assert.Equal(
            "game.crash",
            options.FilePath);
    }

    [Fact]
    public void Parse_CheckFlagSelectsCheckMode()
    {
        CommandLineOptions options =
            ParseSuccessfully(
                "--check",
                "game.crash");

        Assert.Equal(
            CommandMode.Check,
            options.Mode);

        Assert.Equal(
            "game.crash",
            options.FilePath);
    }

    [Fact]
    public void Parse_NoColorDisablesColor()
    {
        CommandLineOptions options =
            ParseSuccessfully(
                "--no-color",
                "--check",
                "game.crash");

        Assert.False(options.UseColor);
    }

    [Fact]
    public void Parse_RejectsUnknownOption()
    {
        CommandLineParseResult result =
            CommandLineParser.Parse(
                ["--unknown", "game.crash"],
                defaultUseColor: true);

        Assert.False(result.Success);

        Assert.Contains(
            "--unknown",
            result.Error);
    }

    [Fact]
    public void Parse_RejectsMissingFile()
    {
        CommandLineParseResult result =
            CommandLineParser.Parse(
                ["--check"],
                defaultUseColor: true);

        Assert.False(result.Success);

        Assert.Contains(
            "requires",
            result.Error);
    }

    [Fact]
    public void Parse_RejectsConflictingModes()
    {
        CommandLineParseResult result =
            CommandLineParser.Parse(
                [
                    "--tokens",
                    "--ast",
                    "game.crash"
                ],
                defaultUseColor: true);

        Assert.False(result.Success);

        Assert.Contains(
            "cannot be combined",
            result.Error);
    }

    [Fact]
    public void Parse_RejectsMultipleFiles()
    {
        CommandLineParseResult result =
            CommandLineParser.Parse(
                [
                    "first.crash",
                    "second.crash"
                ],
                defaultUseColor: true);

        Assert.False(result.Success);

        Assert.Contains(
            "Only one",
            result.Error);
    }

    [Fact]
    public void Parse_HelpDoesNotAcceptFile()
    {
        CommandLineParseResult result =
            CommandLineParser.Parse(
                [
                    "--help",
                    "game.crash"
                ],
                defaultUseColor: true);

        Assert.False(result.Success);

        Assert.Contains(
            "does not accept",
            result.Error);
    }

    private static CommandLineOptions ParseSuccessfully(
        params string[] arguments)
    {
        CommandLineParseResult result =
            CommandLineParser.Parse(
                arguments,
                defaultUseColor: true);

        Assert.True(
            result.Success,
            result.Error);

        return Assert.IsType<CommandLineOptions>(
            result.Options);
    }
}