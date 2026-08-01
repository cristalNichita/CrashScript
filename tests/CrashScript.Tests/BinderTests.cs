using CrashScript.Language.Binding;
using CrashScript.Language.Binding.Nodes;
using CrashScript.Language.Binding.Symbols;
using CrashScript.Language.Diagnostics;
using CrashScript.Language.Lexing;
using CrashScript.Language.Parsing;
using CrashScript.Language.Source;

namespace CrashScript.Tests;

public sealed class BinderTests
{
    [Fact]
    public void Bind_IntegerAdditionProducesInteger()
    {
        BoundExpression expression =
            BindSingleExpression("10 + 20;");

        BoundBinaryExpression binary =
            Assert.IsType<BoundBinaryExpression>(
                expression);

        Assert.Equal(
            TypeSymbol.Int,
            binary.Type);

        Assert.Equal(
            BoundBinaryOperatorKind.Addition,
            binary.OperatorKind);
    }

    [Fact]
    public void Bind_MixedAdditionProducesFloat()
    {
        BoundExpression expression =
            BindSingleExpression("10 + 2.5;");

        BoundBinaryExpression binary =
            Assert.IsType<BoundBinaryExpression>(
                expression);

        Assert.Equal(
            TypeSymbol.Float,
            binary.Type);

        Assert.IsType<BoundConversionExpression>(
            binary.Left);
    }

    [Fact]
    public void Bind_StringAdditionProducesString()
    {
        BoundExpression expression =
            BindSingleExpression(
                "\"Crash\" + \"Script\";");

        BoundBinaryExpression binary =
            Assert.IsType<BoundBinaryExpression>(
                expression);

        Assert.Equal(
            TypeSymbol.String,
            binary.Type);
    }

    [Fact]
    public void Bind_IntegerDivisionProducesFloat()
    {
        BoundExpression expression =
            BindSingleExpression("10 / 4;");

        BoundBinaryExpression binary =
            Assert.IsType<BoundBinaryExpression>(
                expression);

        Assert.Equal(
            TypeSymbol.Float,
            binary.Type);

        Assert.IsType<BoundConversionExpression>(
            binary.Left);

        Assert.IsType<BoundConversionExpression>(
            binary.Right);
    }

    [Fact]
    public void Bind_BooleanNotProducesBoolean()
    {
        BoundExpression expression =
            BindSingleExpression("not false;");

        BoundUnaryExpression unary =
            Assert.IsType<BoundUnaryExpression>(
                expression);

        Assert.Equal(
            TypeSymbol.Bool,
            unary.Type);
    }

    [Fact]
    public void Bind_InvalidUnaryOperatorReportsError()
    {
        BindResult result = Bind("not 42;");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                item.Code ==
                DiagnosticCodes.UnaryOperatorNotDefined);

        Assert.Equal(
            DiagnosticCategory.Type,
            diagnostic.Category);
    }

    [Fact]
    public void Bind_InvalidBinaryOperatorReportsError()
    {
        BindResult result = Bind(
            "\"Crash\" - 10;");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                item.Code ==
                DiagnosticCodes.BinaryOperatorNotDefined);

        Assert.Equal(
            DiagnosticCategory.Type,
            diagnostic.Category);
    }

    [Fact]
    public void Bind_RandomWithNoArgumentsReturnsFloat()
    {
        BoundExpression expression =
            BindSingleExpression("random();");

        BoundCallExpression call =
            Assert.IsType<BoundCallExpression>(
                expression);

        Assert.Equal(
            TypeSymbol.Float,
            call.Type);

        Assert.Empty(call.Arguments);
    }

    [Fact]
    public void Bind_RandomWithIntegerArgumentsReturnsInteger()
    {
        BoundExpression expression =
            BindSingleExpression(
                "random(1, 10);");

        BoundCallExpression call =
            Assert.IsType<BoundCallExpression>(
                expression);

        Assert.Equal(
            TypeSymbol.Int,
            call.Type);

        Assert.Equal(
            2,
            call.Arguments.Count);
    }

    [Fact]
    public void Bind_LogAcceptsAnyValue()
    {
        BoundExpression expression =
            BindSingleExpression(
                "log(42);");

        BoundCallExpression call =
            Assert.IsType<BoundCallExpression>(
                expression);

        Assert.Equal(
            TypeSymbol.Void,
            call.Type);
    }

    [Fact]
    public void Bind_WrongArgumentTypesReportError()
    {
        BindResult result = Bind(
            "random(\"one\", 10);");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                item.Code ==
                DiagnosticCodes.NoMatchingOverload);

        Assert.Contains(
            "string",
            diagnostic.Message);
    }

    [Fact]
    public void Bind_UnknownFunctionReportsError()
    {
        BindResult result = Bind(
            "unknownFunction();");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                item.Code ==
                DiagnosticCodes.UndefinedName);

        Assert.Contains(
            "unknownFunction",
            diagnostic.Message);
    }

    [Fact]
    public void Bind_NullCoalescingRemovesNullableType()
    {
        BoundExpression expression =
            BindSingleExpression(
                "toInt(\"42\") ?? 0;");

        BoundBinaryExpression binary =
            Assert.IsType<BoundBinaryExpression>(
                expression);

        Assert.Equal(
            TypeSymbol.Int,
            binary.Type);

        Assert.Equal(
            BoundBinaryOperatorKind.NullCoalescing,
            binary.OperatorKind);
    }

    private static BoundExpression
        BindSingleExpression(string text)
    {
        BindResult result = Bind(text);

        Assert.Empty(result.Diagnostics);

        BoundExpressionStatement statement =
            Assert.IsType<BoundExpressionStatement>(
                Assert.Single(
                    result.Root.Statements));

        return statement.Expression;
    }

    private static BindResult Bind(string text)
    {
        var source = new SourceText(
            "test.crash",
            text);

        var lexer = new Lexer(source);
        LexResult lexResult = lexer.Lex();

        Assert.Empty(lexResult.Diagnostics);

        var parser = new Parser(
            lexResult.Tokens);

        ParseResult parseResult =
            parser.Parse();

        Assert.Empty(parseResult.Diagnostics);

        var binder = new Binder();

        return binder.Bind(
            parseResult.Root);
    }
}