using CrashScript.Language;
using CrashScript.Language.Binding;
using CrashScript.Language.Diagnostics;
using CrashScript.Language.Parsing;
using CrashScript.Language.Runtime;
using CrashScript.Language.Source;
using CrashScript.Language.Syntax.Statements;

namespace CrashScript.Tests;

public sealed class ControlFlowTests
{
    [Fact]
    public void Parse_ParsesIfElseIfElse()
    {
        ParseResult result = Parse(
            """
            if score >= 100 then
                log("Legendary");
            else if score >= 50 then
                log("High");
            else
                log("Normal");
            end;
            """);

        Assert.Empty(result.Diagnostics);

        IfStatementSyntax statement =
            Assert.IsType<IfStatementSyntax>(
                Assert.Single(
                    result.Root.Statements));

        Assert.Equal(2, statement.Branches.Count);
        Assert.NotNull(statement.ElseClause);
    }

    [Fact]
    public void Parse_ParsesWhileStatement()
    {
        ParseResult result = Parse(
            """
            while score < 10 do
                score = score + 1;
            end;
            """);

        Assert.Empty(result.Diagnostics);

        WhileStatementSyntax statement =
            Assert.IsType<WhileStatementSyntax>(
                Assert.Single(
                    result.Root.Statements));

        Assert.Single(statement.Body.Statements);
    }

    [Fact]
    public void Parse_ReportsMissingThen()
    {
        ParseResult result = Parse(
            """
            if true
                log("Hello");
            end;
            """);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                item.Code ==
                DiagnosticCodes.ExpectedThen);

        Assert.Equal(
            DiagnosticCategory.Parser,
            diagnostic.Category);
    }

    [Fact]
    public void Parse_ReportsMissingDo()
    {
        ParseResult result = Parse(
            """
            while true
                log("Hello");
            end;
            """);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                item.Code ==
                DiagnosticCodes.ExpectedDo);

        Assert.Equal(
            DiagnosticCategory.Parser,
            diagnostic.Category);
    }

    [Fact]
    public void Bind_RejectsNonBooleanIfCondition()
    {
        BindResult result = Bind(
            """
            if 10 then
                log("Wrong");
            end;
            """);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                item.Code ==
                DiagnosticCodes.ConditionMustBeBoolean);

        Assert.Contains(
            "int",
            diagnostic.Message);
    }

    [Fact]
    public void Bind_RejectsNonBooleanWhileCondition()
    {
        BindResult result = Bind(
            """
            while "yes" do
                log("Wrong");
            end;
            """);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                item.Code ==
                DiagnosticCodes.ConditionMustBeBoolean);

        Assert.Contains(
            "string",
            diagnostic.Message);
    }

    [Fact]
    public void Bind_BlockVariableIsNotVisibleOutside()
    {
        BindResult result = Bind(
            """
            if true then
                memory secret := 42;
            end;

            secret;
            """);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                item.Code ==
                DiagnosticCodes.UndefinedName);

        Assert.Contains(
            "secret",
            diagnostic.Message);
    }

    [Fact]
    public void Bind_AllowsShadowingInNestedScope()
    {
        BindResult result = Bind(
            """
            memory value := 1;

            if true then
                memory value := 2;
                log(value);
            end;
            """);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Execute_IfExecutesFirstBranch()
    {
        ExecutionResult result = Execute(
            """
            memory result := 0;

            if true then
                result = 1;
            else
                result = 2;
            end;

            result;
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(1L, result.LastValue);
    }

    [Fact]
    public void Execute_IfExecutesElseIfBranch()
    {
        ExecutionResult result = Execute(
            """
            memory score := 75;
            memory result := "None";

            if score >= 100 then
                result = "Legendary";
            else if score >= 50 then
                result = "High";
            else
                result = "Normal";
            end;

            result;
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Equal("High", result.LastValue);
    }

    [Fact]
    public void Execute_IfExecutesElseBranch()
    {
        ExecutionResult result = Execute(
            """
            memory score := 5;
            memory result := "None";

            if score >= 100 then
                result = "Legendary";
            else if score >= 50 then
                result = "High";
            else
                result = "Normal";
            end;

            result;
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Equal("Normal", result.LastValue);
    }

    [Fact]
    public void Execute_WhileRepeatsBody()
    {
        ExecutionResult result = Execute(
            """
            memory counter := 0;

            while counter < 5 do
                counter = counter + 1;
            end;

            counter;
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(5L, result.LastValue);
    }

    [Fact]
    public void Execute_AssignmentInsideBlockUpdatesOuterVariable()
    {
        ExecutionResult result = Execute(
            """
            memory value := 1;

            if true then
                value = 5;
            end;

            value;
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(5L, result.LastValue);
    }

    [Fact]
    public void Execute_BlockShadowingPreservesOuterValue()
    {
        ExecutionResult result = Execute(
            """
            memory value := 1;

            if true then
                memory value := 2;
                value = 3;
            end;

            value;
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(1L, result.LastValue);
    }

    [Fact]
    public void Execute_WhileCreatesFreshScopePerIteration()
    {
        ExecutionResult result = Execute(
            """
            memory counter := 0;

            while counter < 3 do
                memory temporary := counter;
                counter = counter + 1;
            end;

            counter;
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(3L, result.LastValue);
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