using System.Text;
using CrashScript.Language;
using CrashScript.Language.Runtime;
using Xunit;

namespace CrashScript.Tests;

public sealed class CrashDungeonTests
{
    [Fact]
    public void CrashDungeon_ExecutesWithoutLanguageErrors()
    {
        string repositoryRoot =
            FindRepositoryRoot();

        string filePath = Path.Combine(
            repositoryRoot,
            "examples",
            "crash-dungeon.crash");

        var engine = new CrashScriptEngine();

        var console = new GameTestConsole(
            "Tester",
            "1");

        ExecutionResult result =
            engine.ExecuteFile(
                filePath,
                console,
                new Random(12345));

        Assert.Empty(result.Diagnostics);

        Assert.Contains(
            "CRASH DUNGEON",
            console.Output);

        Assert.Contains(
            "FINAL REPORT",
            console.Output);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (directory is not null)
        {
            string solutionPath =
                Path.Combine(
                    directory.FullName,
                    "CrashScript.sln");

            if (File.Exists(solutionPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the CrashScript repository root.");
    }

    private sealed class GameTestConsole : ICrashConsole
    {
        private readonly Queue<string> _input = new();
        private readonly StringBuilder _output = new();

        public GameTestConsole(
            params string[] input)
        {
            foreach (string item in input)
            {
                _input.Enqueue(item);
            }
        }

        public string Output =>
            _output.ToString();

        public void Write(string text)
        {
            _output.Append(text);
        }

        public void WriteLine(string text)
        {
            _output.AppendLine(text);
        }

        public string? ReadLine()
        {
            if (_input.Count > 0)
            {
                return _input.Dequeue();
            }

            // Always attack when the prepared input ends.
            return "1";
        }
    }
}