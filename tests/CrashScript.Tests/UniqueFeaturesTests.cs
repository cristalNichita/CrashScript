using CrashScript.Language;
using CrashScript.Language.Binding;
using CrashScript.Language.Binding.Nodes;
using CrashScript.Language.Binding.Symbols;
using CrashScript.Language.Diagnostics;
using CrashScript.Language.Parsing;
using CrashScript.Language.Runtime;
using CrashScript.Language.Source;
using CrashScript.Language.Syntax.Expressions;
using CrashScript.Language.Syntax.Statements;

namespace CrashScript.Tests;

public sealed class UniqueFeaturesTests
{
    [Fact]
    public void Parse_ParsesSelectExpression()
    {
        ParseResult result = Parse(
            """
            select
                when score >= 100 => "Legendary";
                when score >= 50 => "High";
                else => "Normal";
            end;
            """);

        Assert.Empty(result.Diagnostics);

        ExpressionStatementSyntax statement =
            Assert.IsType<ExpressionStatementSyntax>(
                Assert.Single(
                    result.Root.Statements));

        SelectExpressionSyntax select =
            Assert.IsType<SelectExpressionSyntax>(
                statement.Expression);

        Assert.Equal(2, select.Branches.Count);
        Assert.NotNull(select.ElseClause);
    }

    [Fact]
    public void Parse_SelectRequiresElseBranch()
    {
        ParseResult result = Parse(
            """
            select
                when true => 10;
            end;
            """);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                item.Code ==
                DiagnosticCodes.ExpectedSelectElse);

        Assert.Equal(
            DiagnosticCategory.Parser,
            diagnostic.Category);
    }

    [Fact]
    public void Parse_ParsesGuardStatement()
    {
        ParseResult result = Parse(
            """
            guard health >= 0
                else "Health cannot be negative";
            """);

        Assert.Empty(result.Diagnostics);

        Assert.IsType<GuardStatementSyntax>(
            Assert.Single(
                result.Root.Statements));
    }

    [Fact]
    public void Bind_SelectReturnsCommonStringType()
    {
        BoundExpression expression =
            BindSingleExpression(
                """
                select
                    when true => "First";
                    else => "Second";
                end;
                """);

        BoundSelectExpression select =
            Assert.IsType<BoundSelectExpression>(
                expression);

        Assert.Equal(
            TypeSymbol.String,
            select.Type);
    }

    [Fact]
    public void Bind_SelectPromotesIntegersToFloat()
    {
        BoundExpression expression =
            BindSingleExpression(
                """
                select
                    when true => 10;
                    else => 2.5;
                end;
                """);

        BoundSelectExpression select =
            Assert.IsType<BoundSelectExpression>(
                expression);

        Assert.Equal(
            TypeSymbol.Float,
            select.Type);

        Assert.IsType<BoundConversionExpression>(
            select.Branches[0].Value);
    }

    [Fact]
    public void Bind_SelectRejectsIncompatibleBranchTypes()
    {
        BindResult result = Bind(
            """
            select
                when true => "CrashScript";
                else => 42;
            end;
            """);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                item.Code ==
                DiagnosticCodes.SelectBranchTypeMismatch);

        Assert.Contains(
            "string",
            diagnostic.Message);

        Assert.Contains(
            "int",
            diagnostic.Message);
    }

    [Fact]
    public void Bind_SelectConditionMustBeBoolean()
    {
        BindResult result = Bind(
            """
            select
                when 10 => "Wrong";
                else => "Fallback";
            end;
            """);

        Assert.Contains(
            result.Diagnostics,
            item =>
                item.Code ==
                DiagnosticCodes.ConditionMustBeBoolean);
    }

    [Fact]
    public void Bind_GuardConditionMustBeBoolean()
    {
        BindResult result = Bind(
            """
            guard 10 else "Wrong condition";
            """);

        Assert.Contains(
            result.Diagnostics,
            item =>
                item.Code ==
                DiagnosticCodes.ConditionMustBeBoolean);
    }

    [Fact]
    public void Bind_GuardMessageMustBeString()
    {
        BindResult result = Bind(
            """
            guard true else 42;
            """);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                item.Code ==
                DiagnosticCodes.GuardMessageMustBeString);

        Assert.Contains(
            "int",
            diagnostic.Message);
    }

    [Fact]
    public void Execute_SelectReturnsFirstMatchingBranch()
    {
        ExecutionResult result = Execute(
            """
            memory score := 75;

            select
                when score >= 100 => "Legendary";
                when score >= 50 => "High";
                else => "Normal";
            end;
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Equal("High", result.LastValue);
    }

    [Fact]
    public void Execute_SelectReturnsElseBranch()
    {
        ExecutionResult result = Execute(
            """
            memory score := 5;

            select
                when score >= 100 => "Legendary";
                when score >= 50 => "High";
                else => "Normal";
            end;
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Equal("Normal", result.LastValue);
    }

    [Fact]
    public void Execute_SelectEvaluatesOnlyChosenValue()
    {
        ExecutionResult result = Execute(
            """
            select
                when true => 10;
                else => 10 / 0;
            end;
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(10.0, result.LastValue);
    }

    [Fact]
    public void Execute_GuardAllowsExecution()
    {
        ExecutionResult result = Execute(
            """
            guard 10 > 0 else "Impossible";

            42;
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(42L, result.LastValue);
    }

    [Fact]
    public void Execute_GuardFailureReportsRuntimeError()
    {
        ExecutionResult result = Execute(
            """
            guard false
                else "Health cannot be negative";
            """);

        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics);

        Assert.Equal(
            DiagnosticCodes.GuardFailed,
            diagnostic.Code);

        Assert.Contains(
            "Health cannot be negative",
            diagnostic.Message);
    }

    [Fact]
    public void Execute_SelectWorksInsideProcess()
    {
        ExecutionResult result = Execute(
            """
            process getRank(score: int) -> string
                return select
                    when score >= 100 => "Legendary";
                    when score >= 50 => "High";
                    else => "Normal";
                end;
            end;

            getRank(75);
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Equal("High", result.LastValue);
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

    private static BoundExpression BindSingleExpression(
        string text)
    {
        BindResult result = Bind(text);

        Assert.Empty(result.Diagnostics);

        BoundExpressionStatement statement =
            Assert.IsType<BoundExpressionStatement>(
                Assert.Single(
                    result.Root.Statements));

        return statement.Expression;
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