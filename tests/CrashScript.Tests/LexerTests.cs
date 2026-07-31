using CrashScript.Language.Diagnostics;
using CrashScript.Language.Lexing;
using CrashScript.Language.Source;

namespace CrashScript.Tests;

public sealed class LexerTests
{
    [Fact]
    public void Lex_LexesVariableDeclaration()
    {
        LexResult result = Lex(
            "memory score: int = 10;");

        Assert.Empty(result.Diagnostics);

        AssertTokenTypes(
            result,
            TokenType.MemoryKeyword,
            TokenType.Identifier,
            TokenType.Colon,
            TokenType.IntKeyword,
            TokenType.Equal,
            TokenType.IntegerLiteral,
            TokenType.Semicolon,
            TokenType.EndOfFile);

        Token integerToken = result.Tokens[5];

        Assert.Equal("10", integerToken.Text);
        Assert.Equal(10L, integerToken.Value);
    }

    [Fact]
    public void Lex_LexesIntegerAndFloatLiterals()
    {
        LexResult result = Lex(
            "10 12.5 0 0.25");

        Assert.Empty(result.Diagnostics);

        AssertTokenTypes(
            result,
            TokenType.IntegerLiteral,
            TokenType.FloatLiteral,
            TokenType.IntegerLiteral,
            TokenType.FloatLiteral,
            TokenType.EndOfFile);

        Assert.Equal(10L, result.Tokens[0].Value);
        Assert.Equal(12.5, result.Tokens[1].Value);
        Assert.Equal(0L, result.Tokens[2].Value);
        Assert.Equal(0.25, result.Tokens[3].Value);
    }

    [Fact]
    public void Lex_LexesCompoundOperators()
    {
        LexResult result = Lex(
            "a := b ?? c != d >= e <= f == g -> h => i;");

        Assert.Empty(result.Diagnostics);

        AssertTokenTypes(
            result,
            TokenType.Identifier,
            TokenType.ColonEqual,
            TokenType.Identifier,
            TokenType.QuestionQuestion,
            TokenType.Identifier,
            TokenType.BangEqual,
            TokenType.Identifier,
            TokenType.GreaterEqual,
            TokenType.Identifier,
            TokenType.LessEqual,
            TokenType.Identifier,
            TokenType.EqualEqual,
            TokenType.Identifier,
            TokenType.Arrow,
            TokenType.Identifier,
            TokenType.FatArrow,
            TokenType.Identifier,
            TokenType.Semicolon,
            TokenType.EndOfFile);
    }

    [Fact]
    public void Lex_RecognizesKeywords()
    {
        const string source = """
            memory fixed process return
            if else while end
            and or not
            select when guard
            true false null
            int float string bool void
            """;

        LexResult result = Lex(source);

        Assert.Empty(result.Diagnostics);

        AssertTokenTypes(
            result,
            TokenType.MemoryKeyword,
            TokenType.FixedKeyword,
            TokenType.ProcessKeyword,
            TokenType.ReturnKeyword,

            TokenType.IfKeyword,
            TokenType.ElseKeyword,
            TokenType.WhileKeyword,
            TokenType.EndKeyword,

            TokenType.AndKeyword,
            TokenType.OrKeyword,
            TokenType.NotKeyword,

            TokenType.SelectKeyword,
            TokenType.WhenKeyword,
            TokenType.GuardKeyword,

            TokenType.TrueKeyword,
            TokenType.FalseKeyword,
            TokenType.NullKeyword,

            TokenType.IntKeyword,
            TokenType.FloatKeyword,
            TokenType.StringKeyword,
            TokenType.BoolKeyword,
            TokenType.VoidKeyword,

            TokenType.EndOfFile);
    }

    [Fact]
    public void Lex_IgnoresLineComments()
    {
        const string source = """
            memory first: int = 1; // first value
            // Entire line comment
            memory second: int = 2;
            """;

        LexResult result = Lex(source);

        Assert.Empty(result.Diagnostics);

        Token[] identifiers = result.Tokens
            .Where(token => token.Type == TokenType.Identifier)
            .ToArray();

        Assert.Equal(2, identifiers.Length);
        Assert.Equal("first", identifiers[0].Text);
        Assert.Equal("second", identifiers[1].Text);
    }

    [Fact]
    public void Lex_DecodesStringEscapeSequences()
    {
        const string source =
            "log(\"Line 1\\nLine 2\\t\\\"Crash\\\"\\\\\");";

        LexResult result = Lex(source);

        Assert.Empty(result.Diagnostics);

        Token stringToken = Assert.Single(result.Tokens, token => token.Type == TokenType.StringLiteral);

        Assert.Equal(
            "Line 1\nLine 2\t\"Crash\"\\",
            stringToken.Value);
    }

    [Fact]
    public void Lex_ReportsInvalidEscapeSequence()
    {
        const string source =
            "log(\"Crash\\qScript\");";

        LexResult result = Lex(source);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);

        Assert.Equal(
            DiagnosticCodes.InvalidEscapeSequence,
            diagnostic.Code);

        Assert.Contains(
            "\\q",
            diagnostic.Message);
    }

    [Fact]
    public void Lex_ReportsUnterminatedString()
    {
        const string source =
            "memory name: string = \"Crash";

        LexResult result = Lex(source);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);

        Assert.Equal(
            DiagnosticCodes.UnterminatedString,
            diagnostic.Code);
    }

    [Fact]
    public void Lex_ReportsUnexpectedCharacter()
    {
        const string source =
            "memory score: int = @;";

        LexResult result = Lex(source);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);

        Assert.Equal(
            DiagnosticCodes.UnexpectedCharacter,
            diagnostic.Code);

        Assert.Equal("@", diagnostic.Span.Text);
    }

    [Fact]
    public void Lex_TracksTokenLineAndColumn()
    {
        const string source = """

            memory score: int = 10;
            """;

        LexResult result = Lex(source);

        Token memoryToken = result.Tokens[0];

        Assert.Equal(2, memoryToken.Span.StartLocation.Line);
        Assert.Equal(1, memoryToken.Span.StartLocation.Column);
    }

    private static LexResult Lex(string text)
    {
        var source = new SourceText(
            "test.crash",
            text);

        var lexer = new Lexer(source);

        return lexer.Lex();
    }

    private static void AssertTokenTypes(
        LexResult result,
        params TokenType[] expectedTypes)
    {
        TokenType[] actualTypes = result.Tokens
            .Select(token => token.Type)
            .ToArray();

        Assert.Equal(expectedTypes, actualTypes);
    }
}