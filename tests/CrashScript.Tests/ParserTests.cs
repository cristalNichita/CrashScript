using CrashScript.Language.Diagnostics;
using CrashScript.Language.Lexing;
using CrashScript.Language.Parsing;
using CrashScript.Language.Source;
using CrashScript.Language.Syntax.Expressions;
using CrashScript.Language.Syntax.Statements;
using Xunit;

namespace CrashScript.Tests;

public sealed class ParserTests
{
    [Fact]
    public void Parse_ParsesIntegerLiteral()
    {
        ParseResult result = Parse("42;");

        Assert.Empty(result.Diagnostics);

        ExpressionStatementSyntax statement =
            Assert.IsType<ExpressionStatementSyntax>(
                Assert.Single(result.Root.Statements));

        LiteralExpressionSyntax literal =
            Assert.IsType<LiteralExpressionSyntax>(
                statement.Expression);

        Assert.Equal(42L, literal.Value);
    }

    [Fact]
    public void Parse_MultiplicationHasHigherPrecedenceThanAddition()
    {
        ParseResult result = Parse("1 + 2 * 3;");

        BinaryExpressionSyntax addition =
            GetSingleBinaryExpression(result);

        Assert.Equal(
            TokenType.Plus,
            addition.OperatorToken.Type);

        Assert.IsType<LiteralExpressionSyntax>(
            addition.Left);

        BinaryExpressionSyntax multiplication =
            Assert.IsType<BinaryExpressionSyntax>(
                addition.Right);

        Assert.Equal(
            TokenType.Star,
            multiplication.OperatorToken.Type);
    }

    [Fact]
    public void Parse_ParenthesesOverridePrecedence()
    {
        ParseResult result = Parse("(1 + 2) * 3;");

        BinaryExpressionSyntax multiplication =
            GetSingleBinaryExpression(result);

        Assert.Equal(
            TokenType.Star,
            multiplication.OperatorToken.Type);

        ParenthesizedExpressionSyntax parenthesized =
            Assert.IsType<ParenthesizedExpressionSyntax>(
                multiplication.Left);

        BinaryExpressionSyntax addition =
            Assert.IsType<BinaryExpressionSyntax>(
                parenthesized.Expression);

        Assert.Equal(
            TokenType.Plus,
            addition.OperatorToken.Type);
    }

    [Fact]
    public void Parse_UnaryOperatorHasHigherPrecedenceThanOr()
    {
        ParseResult result = Parse(
            "not true or false;");

        BinaryExpressionSyntax orExpression =
            GetSingleBinaryExpression(result);

        Assert.Equal(
            TokenType.OrKeyword,
            orExpression.OperatorToken.Type);

        UnaryExpressionSyntax notExpression =
            Assert.IsType<UnaryExpressionSyntax>(
                orExpression.Left);

        Assert.Equal(
            TokenType.NotKeyword,
            notExpression.OperatorToken.Type);
    }

    [Fact]
    public void Parse_AndHasHigherPrecedenceThanOr()
    {
        ParseResult result = Parse(
            "true or false and true;");

        BinaryExpressionSyntax orExpression =
            GetSingleBinaryExpression(result);

        Assert.Equal(
            TokenType.OrKeyword,
            orExpression.OperatorToken.Type);

        BinaryExpressionSyntax andExpression =
            Assert.IsType<BinaryExpressionSyntax>(
                orExpression.Right);

        Assert.Equal(
            TokenType.AndKeyword,
            andExpression.OperatorToken.Type);
    }

    [Fact]
    public void Parse_NullCoalescingIsRightAssociative()
    {
        ParseResult result = Parse(
            "first ?? second ?? fallback;");

        BinaryExpressionSyntax outer =
            GetSingleBinaryExpression(result);

        Assert.Equal(
            TokenType.QuestionQuestion,
            outer.OperatorToken.Type);

        NameExpressionSyntax left =
            Assert.IsType<NameExpressionSyntax>(
                outer.Left);

        Assert.Equal("first", left.Name);

        BinaryExpressionSyntax right =
            Assert.IsType<BinaryExpressionSyntax>(
                outer.Right);

        Assert.Equal(
            TokenType.QuestionQuestion,
            right.OperatorToken.Type);
    }

    [Fact]
    public void Parse_ParsesFunctionCallArguments()
    {
        ParseResult result = Parse(
            "random(1, 10);");

        ExpressionStatementSyntax statement =
            Assert.IsType<ExpressionStatementSyntax>(
                Assert.Single(result.Root.Statements));

        CallExpressionSyntax call =
            Assert.IsType<CallExpressionSyntax>(
                statement.Expression);

        NameExpressionSyntax callee =
            Assert.IsType<NameExpressionSyntax>(
                call.Callee);

        Assert.Equal("random", callee.Name);
        Assert.Equal(2, call.Arguments.Count);
    }

    [Fact]
    public void Parse_ParsesNestedFunctionCalls()
    {
        ParseResult result = Parse(
            "log(toString(42));");

        ExpressionStatementSyntax statement =
            Assert.IsType<ExpressionStatementSyntax>(
                Assert.Single(result.Root.Statements));

        CallExpressionSyntax logCall =
            Assert.IsType<CallExpressionSyntax>(
                statement.Expression);

        CallExpressionSyntax toStringCall =
            Assert.IsType<CallExpressionSyntax>(
                Assert.Single(logCall.Arguments));

        NameExpressionSyntax callee =
            Assert.IsType<NameExpressionSyntax>(
                toStringCall.Callee);

        Assert.Equal("toString", callee.Name);
    }

    [Fact]
    public void Parse_ParsesMultipleStatements()
    {
        ParseResult result = Parse(
            "1; 2; 3;");

        Assert.Empty(result.Diagnostics);
        Assert.Equal(3, result.Root.Statements.Count);
    }

    [Fact]
    public void Parse_ReportsMissingSemicolon()
    {
        ParseResult result = Parse(
            "1 + 2");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                    item.Code ==
                    DiagnosticCodes.ExpectedSemicolon);

        Assert.Equal(
            DiagnosticCategory.Parser,
            diagnostic.Category);
    }

    [Fact]
    public void Parse_ReportsMissingExpression()
    {
        ParseResult result = Parse(
            "1 + ;");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
                    item.Code ==
                    DiagnosticCodes.ExpectedExpression);

        Assert.Equal(
            DiagnosticCategory.Parser,
            diagnostic.Category);
    }

    private static ParseResult Parse(string text)
    {
        var source = new SourceText(
            "test.crash",
            text);

        var lexer = new Lexer(source);
        LexResult lexResult = lexer.Lex();

        Assert.Empty(lexResult.Diagnostics);

        var parser = new Parser(lexResult.Tokens);

        return parser.Parse();
    }

    private static BinaryExpressionSyntax
        GetSingleBinaryExpression(ParseResult result)
    {
        Assert.Empty(result.Diagnostics);

        ExpressionStatementSyntax statement =
            Assert.IsType<ExpressionStatementSyntax>(
                Assert.Single(result.Root.Statements));

        return Assert.IsType<BinaryExpressionSyntax>(
            statement.Expression);
    }
}