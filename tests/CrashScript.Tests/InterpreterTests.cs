using System.Text;
using CrashScript.Language.Binding;
using CrashScript.Language.Diagnostics;
using CrashScript.Language.Lexing;
using CrashScript.Language.Parsing;
using CrashScript.Language.Runtime;
using CrashScript.Language.Source;
using Xunit;

namespace CrashScript.Tests;

public sealed class InterpreterTests
{
    [Fact]
    public void Execute_EvaluatesIntegerArithmetic()
    {
        ExecutionResult result = Execute(
            "1 + 2 * 3;");

        Assert.Empty(result.Diagnostics);
        Assert.Equal(7L, result.LastValue);
    }

    [Fact]
    public void Execute_EvaluatesMixedArithmetic()
    {
        ExecutionResult result = Execute(
            "10 + 2.5;");

        Assert.Empty(result.Diagnostics);
        Assert.Equal(12.5, result.LastValue);
    }

    [Fact]
    public void Execute_IntegerDivisionReturnsFloat()
    {
        ExecutionResult result = Execute(
            "10 / 4;");

        Assert.Empty(result.Diagnostics);
        Assert.Equal(2.5, result.LastValue);
    }

    [Fact]
    public void Execute_ConcatenatesStrings()
    {
        ExecutionResult result = Execute(
            "\"Crash\" + \"Script\";");

        Assert.Empty(result.Diagnostics);
        Assert.Equal(
            "CrashScript",
            result.LastValue);
    }

    [Fact]
    public void Execute_LogicalAndShortCircuits()
    {
        ExecutionResult result = Execute(
            "false and (10 / 0 > 1);");

        Assert.Empty(result.Diagnostics);
        Assert.Equal(false, result.LastValue);
    }

    [Fact]
    public void Execute_LogicalOrShortCircuits()
    {
        ExecutionResult result = Execute(
            "true or (10 / 0 > 1);");

        Assert.Empty(result.Diagnostics);
        Assert.Equal(true, result.LastValue);
    }

    [Fact]
    public void Execute_NullCoalescingUsesFallback()
    {
        ExecutionResult result = Execute(
            "null ?? \"Fallback\";");

        Assert.Empty(result.Diagnostics);
        Assert.Equal(
            "Fallback",
            result.LastValue);
    }

    [Fact]
    public void Execute_NullCoalescingKeepsParsedValue()
    {
        ExecutionResult result = Execute(
            "toInt(\"42\") ?? 0;");

        Assert.Empty(result.Diagnostics);
        Assert.Equal(42L, result.LastValue);
    }

    [Fact]
    public void Execute_LogWritesFormattedValue()
    {
        var console = new TestCrashConsole();

        ExecutionResult result = Execute(
            "log(10 / 4);",
            console);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(
            $"2.5{Environment.NewLine}",
            console.Output);
    }

    [Fact]
    public void Execute_InputWritesPromptAndReturnsText()
    {
        var console = new TestCrashConsole(
            "Crash");

        ExecutionResult result = Execute(
            "input(\"Name: \");",
            console);

        Assert.Empty(result.Diagnostics);
        Assert.Equal("Crash", result.LastValue);
        Assert.Equal("Name: ", console.Output);
    }

    [Fact]
    public void Execute_LengthReturnsInteger()
    {
        ExecutionResult result = Execute(
            "length(\"CrashScript\");");

        Assert.Empty(result.Diagnostics);
        Assert.Equal(11L, result.LastValue);
    }

    [Fact]
    public void Execute_InvalidToIntReturnsNull()
    {
        ExecutionResult result = Execute(
            "toInt(\"not a number\");");

        Assert.Empty(result.Diagnostics);
        Assert.Null(result.LastValue);
    }

    [Fact]
    public void Execute_RandomIntegerStaysInsideRange()
    {
        ExecutionResult result = Execute(
            "random(5, 10);");

        Assert.Empty(result.Diagnostics);

        long value = Assert.IsType<long>(
            result.LastValue);

        Assert.InRange(value, 5, 10);
    }

    [Fact]
    public void Execute_DivisionByZeroReportsRuntimeError()
    {
        ExecutionResult result = Execute(
            "10 / 0;");

        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics);

        Assert.Equal(
            DiagnosticCodes.DivisionByZero,
            diagnostic.Code);

        Assert.Equal(
            DiagnosticCategory.Runtime,
            diagnostic.Category);
    }

    [Fact]
    public void Execute_RemainderByZeroReportsRuntimeError()
    {
        ExecutionResult result = Execute(
            "10 % 0;");

        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics);

        Assert.Equal(
            DiagnosticCodes.DivisionByZero,
            diagnostic.Code);
    }

    [Fact]
    public void Execute_IntegerOverflowReportsRuntimeError()
    {
        ExecutionResult result = Execute(
            "9223372036854775807 + 1;");

        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics);

        Assert.Equal(
            DiagnosticCodes.NumericOverflow,
            diagnostic.Code);
    }

    [Fact]
    public void Execute_InvalidRandomRangeReportsRuntimeError()
    {
        ExecutionResult result = Execute(
            "random(10, 1);");

        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics);

        Assert.Equal(
            DiagnosticCodes.InvalidRandomRange,
            diagnostic.Code);
    }

    private static ExecutionResult Execute(
        string text,
        TestCrashConsole? console = null)
    {
        var source = new SourceText(
            "test.crash",
            text);

        var lexer = new Lexer(source);
        LexResult lexResult = lexer.Lex();

        Assert.Empty(
            lexResult.Diagnostics);

        var parser = new Parser(
            lexResult.Tokens);

        ParseResult parseResult =
            parser.Parse();

        Assert.Empty(
            parseResult.Diagnostics);

        var binder = new Binder();

        BindResult bindResult =
            binder.Bind(
                parseResult.Root);

        Assert.Empty(
            bindResult.Diagnostics);

        var interpreter = new Interpreter(
            console ?? new TestCrashConsole(),
            new Random(12345));

        return interpreter.Execute(
            bindResult.Root);
    }

    private sealed class TestCrashConsole : ICrashConsole
    {
        private readonly Queue<string?> _input = new();
        private readonly StringBuilder _output = new();

        public TestCrashConsole(
            params string?[] input)
        {
            foreach (string? item in input)
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
            return _input.Count > 0
                ? _input.Dequeue()
                : null;
        }
    }
}