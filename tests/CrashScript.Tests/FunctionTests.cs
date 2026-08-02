using CrashScript.Language;
using CrashScript.Language.Binding;
using CrashScript.Language.Diagnostics;
using CrashScript.Language.Parsing;
using CrashScript.Language.Runtime;
using CrashScript.Language.Source;
using CrashScript.Language.Syntax.Statements;

namespace CrashScript.Tests;

public sealed class FunctionTests
{
    [Fact]
    public void Parse_ParsesProcessDeclaration()
    {
        ParseResult result = Parse(
            """
            process add(a: int, b: int) -> int
                return a + b;
            end;
            """);

        Assert.Empty(result.Diagnostics);

        FunctionDeclarationStatementSyntax declaration =
            Assert.IsType<FunctionDeclarationStatementSyntax>(
                Assert.Single(result.Root.Statements));

        Assert.Equal("add", declaration.Name);
        Assert.Equal(2, declaration.Parameters.Count);
        Assert.Equal("int", declaration.ReturnType.Name);
    }

    [Fact]
    public void Parse_ParsesReturnWithoutValue()
    {
        ParseResult result = Parse(
            """
            process stop() -> void
                return;
            end;
            """);

        Assert.Empty(result.Diagnostics);

        FunctionDeclarationStatementSyntax function =
            Assert.IsType<FunctionDeclarationStatementSyntax>(
                Assert.Single(result.Root.Statements));

        ReturnStatementSyntax returnStatement =
            Assert.IsType<ReturnStatementSyntax>(
                Assert.Single(function.Body.Statements));

        Assert.Null(returnStatement.Expression);
    }

    [Fact]
    public void Bind_AllowsCallBeforeDeclaration()
    {
        BindResult result = Bind(
            """
            add(10, 20);

            process add(a: int, b: int) -> int
                return a + b;
            end;
            """);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Bind_RejectsDuplicateProcess()
    {
        BindResult result = Bind(
            """
            process test() -> void
                return;
            end;

            process test() -> void
                return;
            end;
            """);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                item.Code ==
                DiagnosticCodes.ProcessAlreadyDeclared);

        Assert.Contains("test", diagnostic.Message);
    }

    [Fact]
    public void Bind_RejectsDuplicateParameter()
    {
        BindResult result = Bind(
            """
            process add(value: int, value: int) -> int
                return value;
            end;
            """);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                item.Code ==
                DiagnosticCodes.DuplicateParameter);

        Assert.Contains("value", diagnostic.Message);
    }

    [Fact]
    public void Bind_RejectsReturnOutsideProcess()
    {
        BindResult result = Bind(
            "return 10;");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                item.Code ==
                DiagnosticCodes.ReturnOutsideProcess);

        Assert.Equal(
            DiagnosticCategory.Type,
            diagnostic.Category);
    }

    [Fact]
    public void Bind_RequiresReturnValue()
    {
        BindResult result = Bind(
            """
            process getValue() -> int
                return;
            end;
            """);

        Assert.Contains(
            result.Diagnostics,
            item =>
                item.Code ==
                DiagnosticCodes.ReturnValueRequired);
    }

    [Fact]
    public void Bind_VoidProcessCannotReturnValue()
    {
        BindResult result = Bind(
            """
            process stop() -> void
                return 10;
            end;
            """);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                item.Code ==
                DiagnosticCodes.CannotReturnValue);

        Assert.Contains("void", diagnostic.Message);
    }

    [Fact]
    public void Bind_RequiresReturnOnEveryPath()
    {
        BindResult result = Bind(
            """
            process getValue(condition: bool) -> int
                if condition then
                    return 10;
                end;
            end;
            """);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                item.Code ==
                DiagnosticCodes.MissingReturn);

        Assert.Contains(
            "every path",
            diagnostic.Message);
    }

    [Fact]
    public void Execute_CallsUserProcess()
    {
        ExecutionResult result = Execute(
            """
            process add(a: int, b: int) -> int
                return a + b;
            end;

            add(10, 20);
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(30L, result.LastValue);
    }

    [Fact]
    public void Execute_ProcessUsesLocalScope()
    {
        ExecutionResult result = Execute(
            """
            memory value := 5;

            process calculate(value: int) -> int
                memory doubled := value * 2;
                return doubled;
            end;

            calculate(10);
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(20L, result.LastValue);
    }

    [Fact]
    public void Execute_SupportsRecursion()
    {
        ExecutionResult result = Execute(
            """
            process factorial(number: int) -> int
                if number <= 1 then
                    return 1;
                end;

                return number * factorial(number - 1);
            end;

            factorial(5);
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(120L, result.LastValue);
    }

    private static ParseResult Parse(string text)
    {
        var engine = new CrashScriptEngine();

        return engine.Parse(
            new SourceText(
                "test.crash",
                text));
    }

    private static BindResult Bind(string text)
    {
        var engine = new CrashScriptEngine();

        return engine.Bind(
            new SourceText(
                "test.crash",
                text));
    }

    private static ExecutionResult Execute(string text)
    {
        var engine = new CrashScriptEngine();

        return engine.Execute(
            new SourceText(
                "test.crash",
                text),
            new TestCrashConsole(),
            new Random(12345));
    }

    private sealed class TestCrashConsole : ICrashConsole
    {
        public void Write(string text)
        {
        }

        public void WriteLine(string text)
        {
        }

        public string? ReadLine()
        {
            return null;
        }
    }
}