using System.Text;
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
using Xunit;

namespace CrashScript.Tests;

public sealed class VariablesTests
{
    [Fact]
    public void Parse_ParsesExplicitVariableDeclaration()
    {
        ParseResult result = Parse(
            "memory score: int = 10;");

        Assert.Empty(result.Diagnostics);

        VariableDeclarationStatementSyntax declaration =
            Assert.IsType<VariableDeclarationStatementSyntax>(
                Assert.Single(result.Root.Statements));

        Assert.Equal("score", declaration.Name);
        Assert.False(declaration.IsFixed);
        Assert.False(declaration.UsesTypeInference);
        Assert.Equal(
            "int",
            declaration.DeclaredType?.Name);
    }

    [Fact]
    public void Parse_ParsesInferredFixedDeclaration()
    {
        ParseResult result = Parse(
            "fixed maxScore := 100;");

        Assert.Empty(result.Diagnostics);

        VariableDeclarationStatementSyntax declaration =
            Assert.IsType<VariableDeclarationStatementSyntax>(
                Assert.Single(result.Root.Statements));

        Assert.Equal("maxScore", declaration.Name);
        Assert.True(declaration.IsFixed);
        Assert.True(declaration.UsesTypeInference);
        Assert.Null(declaration.DeclaredType);
    }

    [Fact]
    public void Parse_ParsesAssignmentExpression()
    {
        ParseResult result = Parse(
            "score = score + 1;");

        Assert.Empty(result.Diagnostics);

        ExpressionStatementSyntax statement =
            Assert.IsType<ExpressionStatementSyntax>(
                Assert.Single(result.Root.Statements));

        AssignmentExpressionSyntax assignment =
            Assert.IsType<AssignmentExpressionSyntax>(
                statement.Expression);

        NameExpressionSyntax target =
            Assert.IsType<NameExpressionSyntax>(
                assignment.Target);

        Assert.Equal("score", target.Name);
    }

    [Fact]
    public void Bind_InfersIntegerVariableType()
    {
        BindResult result = Bind(
            "memory score := 10;");

        Assert.Empty(result.Diagnostics);

        BoundVariableDeclaration declaration =
            Assert.IsType<BoundVariableDeclaration>(
                Assert.Single(result.Root.Statements));

        Assert.Equal(
            TypeSymbol.Int,
            declaration.Variable.Type);
    }

    [Fact]
    public void Bind_InsertsIntegerToFloatConversion()
    {
        BindResult result = Bind(
            "memory speed: float = 10;");

        Assert.Empty(result.Diagnostics);

        BoundVariableDeclaration declaration =
            Assert.IsType<BoundVariableDeclaration>(
                Assert.Single(result.Root.Statements));

        Assert.Equal(
            TypeSymbol.Float,
            declaration.Variable.Type);

        Assert.IsType<BoundConversionExpression>(
            declaration.Initializer);
    }

    [Fact]
    public void Bind_RejectsIncompatibleInitializer()
    {
        BindResult result = Bind(
            "memory score: int = \"ten\";");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                item.Code ==
                DiagnosticCodes.CannotConvertType);

        Assert.Contains(
            "string",
            diagnostic.Message);

        Assert.Contains(
            "int",
            diagnostic.Message);
    }

    [Fact]
    public void Bind_RejectsDuplicateVariable()
    {
        BindResult result = Bind(
            """
            memory score := 10;
            memory score := 20;
            """);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                item.Code ==
                DiagnosticCodes.VariableAlreadyDeclared);

        Assert.Contains(
            "score",
            diagnostic.Message);
    }

    [Fact]
    public void Bind_RejectsAssignmentToFixedValue()
    {
        BindResult result = Bind(
            """
            fixed maxHealth := 100;
            maxHealth = 200;
            """);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                item.Code ==
                DiagnosticCodes.CannotAssignFixed);

        Assert.Contains(
            "maxHealth",
            diagnostic.Message);
    }

    [Fact]
    public void Execute_ReadsDeclaredVariable()
    {
        var console = new TestCrashConsole();

        ExecutionResult result = Execute(
            """
            memory score := 10;
            log(score);
            """,
            console);

        Assert.Empty(result.Diagnostics);

        Assert.Equal(
            $"10{Environment.NewLine}",
            console.Output);
    }

    [Fact]
    public void Execute_AssignsNewVariableValue()
    {
        ExecutionResult result = Execute(
            """
            memory score := 10;
            score = score + 5;
            score;
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(15L, result.LastValue);
    }

    [Fact]
    public void Execute_NullableVariableUsesFallback()
    {
        ExecutionResult result = Execute(
            """
            memory nickname: string? = null;
            memory displayName: string =
                nickname ?? "Unknown";
            displayName;
            """);

        Assert.Empty(result.Diagnostics);

        Assert.Equal(
            "Unknown",
            result.LastValue);
    }

    [Fact]
    public void RuntimeEnvironment_FindsParentValue()
    {
        var variable = new VariableSymbol(
            "score",
            TypeSymbol.Int,
            isReadOnly: false);

        var parent =
            new RuntimeEnvironment();

        var child =
            new RuntimeEnvironment(parent);

        parent.Define(variable, 10L);

        Assert.Equal(
            10L,
            child.Get(variable));

        child.Assign(variable, 20L);

        Assert.Equal(
            20L,
            parent.Get(variable));
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

    private static ExecutionResult Execute(
        string text,
        TestCrashConsole? console = null)
    {
        var engine = new CrashScriptEngine();

        return engine.Execute(
            new SourceText(
                "test.crash",
                text),
            console ?? new TestCrashConsole(),
            new Random(12345));
    }

    private sealed class TestCrashConsole : ICrashConsole
    {
        private readonly StringBuilder _output = new();

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
            return null;
        }
    }
}