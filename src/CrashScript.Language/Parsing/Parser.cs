using CrashScript.Language.Diagnostics;
using CrashScript.Language.Lexing;
using CrashScript.Language.Source;
using CrashScript.Language.Syntax;
using CrashScript.Language.Syntax.Expressions;
using CrashScript.Language.Syntax.Statements;

namespace CrashScript.Language.Parsing;

public sealed class Parser
{
    private readonly Token[] _tokens;
    private readonly DiagnosticBag _diagnostics = new();

    private int _position;

    public Parser(IReadOnlyList<Token> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        _tokens = tokens
            .Where(token => token.Type != TokenType.BadToken)
            .ToArray();

        if (_tokens.Length == 0 ||
            _tokens[^1].Type != TokenType.EndOfFile)
        {
            throw new ArgumentException(
                "The parser requires a token stream ending with EndOfFile.",
                nameof(tokens));
        }
    }

    private Token Current => Peek(0);

    private Token Peek(int offset)
    {
        int index = _position + offset;

        if (index >= _tokens.Length)
        {
            return _tokens[^1];
        }

        return _tokens[index];
    }

    public ParseResult Parse()
    {
        var statements = new List<StatementSyntax>();

        while (Current.Type != TokenType.EndOfFile)
        {
            int startPosition = _position;

            statements.Add(ParseStatement());

            if (_position == startPosition)
            {
                NextToken();
            }
        }

        Token endOfFileToken =
            MatchToken(TokenType.EndOfFile);

        var root = new CompilationUnitSyntax(
            statements.ToArray(),
            endOfFileToken);

        return new ParseResult(
            root,
            _diagnostics.ToArray());
    }

    private StatementSyntax ParseStatement()
    {
        return Current.Type switch
        {
            TokenType.MemoryKeyword or
            TokenType.FixedKeyword =>
                ParseVariableDeclarationStatement(),

            _ => ParseExpressionStatement()
        };
    }

    private VariableDeclarationStatementSyntax
        ParseVariableDeclarationStatement()
    {
        Token declarationKeyword = NextToken();

        Token identifierToken =
            MatchToken(TokenType.Identifier);

        Token? colonToken = null;
        TypeSyntax? declaredType = null;
        Token assignmentToken;

        if (Current.Type == TokenType.ColonEqual)
        {
            assignmentToken = NextToken();
        }
        else
        {
            colonToken = MatchToken(
                TokenType.Colon);

            declaredType = ParseTypeSyntax();

            assignmentToken = MatchToken(
                TokenType.Equal);
        }

        ExpressionSyntax initializer =
            ParseExpression();

        Token semicolonToken =
            MatchSemicolon();

        return new VariableDeclarationStatementSyntax(
            declarationKeyword,
            identifierToken,
            colonToken,
            declaredType,
            assignmentToken,
            initializer,
            semicolonToken);
    }

    private TypeSyntax ParseTypeSyntax()
    {
        Token nameToken;

        if (IsTypeNameToken(Current.Type))
        {
            nameToken = NextToken();
        }
        else
        {
            nameToken = MatchToken(
                TokenType.Identifier);
        }

        Token? questionToken = null;

        if (Current.Type == TokenType.Question)
        {
            questionToken = NextToken();
        }

        return new TypeSyntax(
            nameToken,
            questionToken);
    }

    private ExpressionStatementSyntax
        ParseExpressionStatement()
    {
        ExpressionSyntax expression =
            ParseExpression();

        Token semicolonToken =
            MatchSemicolon();

        return new ExpressionStatementSyntax(
            expression,
            semicolonToken);
    }

    private ExpressionSyntax ParseExpression()
    {
        return ParseAssignmentExpression();
    }

    private ExpressionSyntax ParseAssignmentExpression()
    {
        ExpressionSyntax target =
            ParseNullCoalescingExpression();

        if (Current.Type != TokenType.Equal)
        {
            return target;
        }

        Token equalsToken = NextToken();

        ExpressionSyntax value =
            ParseAssignmentExpression();

        return new AssignmentExpressionSyntax(
            target,
            equalsToken,
            value);
    }

    private ExpressionSyntax ParseNullCoalescingExpression()
    {
        ExpressionSyntax left =
            ParseOrExpression();

        if (Current.Type != TokenType.QuestionQuestion)
        {
            return left;
        }

        Token operatorToken = NextToken();

        ExpressionSyntax right =
            ParseNullCoalescingExpression();

        return new BinaryExpressionSyntax(
            left,
            operatorToken,
            right);
    }

    private ExpressionSyntax ParseOrExpression()
    {
        ExpressionSyntax left =
            ParseAndExpression();

        while (Current.Type == TokenType.OrKeyword)
        {
            Token operatorToken = NextToken();

            ExpressionSyntax right =
                ParseAndExpression();

            left = new BinaryExpressionSyntax(
                left,
                operatorToken,
                right);
        }

        return left;
    }

    private ExpressionSyntax ParseAndExpression()
    {
        ExpressionSyntax left =
            ParseEqualityExpression();

        while (Current.Type == TokenType.AndKeyword)
        {
            Token operatorToken = NextToken();

            ExpressionSyntax right =
                ParseEqualityExpression();

            left = new BinaryExpressionSyntax(
                left,
                operatorToken,
                right);
        }

        return left;
    }

    private ExpressionSyntax ParseEqualityExpression()
    {
        ExpressionSyntax left =
            ParseComparisonExpression();

        while (Current.Type is
               TokenType.EqualEqual or
               TokenType.BangEqual)
        {
            Token operatorToken = NextToken();

            ExpressionSyntax right =
                ParseComparisonExpression();

            left = new BinaryExpressionSyntax(
                left,
                operatorToken,
                right);
        }

        return left;
    }

    private ExpressionSyntax ParseComparisonExpression()
    {
        ExpressionSyntax left =
            ParseTermExpression();

        while (Current.Type is
               TokenType.Greater or
               TokenType.GreaterEqual or
               TokenType.Less or
               TokenType.LessEqual)
        {
            Token operatorToken = NextToken();

            ExpressionSyntax right =
                ParseTermExpression();

            left = new BinaryExpressionSyntax(
                left,
                operatorToken,
                right);
        }

        return left;
    }

    private ExpressionSyntax ParseTermExpression()
    {
        ExpressionSyntax left =
            ParseFactorExpression();

        while (Current.Type is
               TokenType.Plus or
               TokenType.Minus)
        {
            Token operatorToken = NextToken();

            ExpressionSyntax right =
                ParseFactorExpression();

            left = new BinaryExpressionSyntax(
                left,
                operatorToken,
                right);
        }

        return left;
    }

    private ExpressionSyntax ParseFactorExpression()
    {
        ExpressionSyntax left =
            ParseUnaryExpression();

        while (Current.Type is
               TokenType.Star or
               TokenType.Slash or
               TokenType.Percent)
        {
            Token operatorToken = NextToken();

            ExpressionSyntax right =
                ParseUnaryExpression();

            left = new BinaryExpressionSyntax(
                left,
                operatorToken,
                right);
        }

        return left;
    }

    private ExpressionSyntax ParseUnaryExpression()
    {
        if (Current.Type is
            TokenType.Minus or
            TokenType.NotKeyword)
        {
            Token operatorToken = NextToken();

            ExpressionSyntax operand =
                ParseUnaryExpression();

            return new UnaryExpressionSyntax(
                operatorToken,
                operand);
        }

        return ParseCallExpression();
    }

    private ExpressionSyntax ParseCallExpression()
    {
        ExpressionSyntax expression =
            ParsePrimaryExpression();

        while (Current.Type ==
               TokenType.LeftParenthesis)
        {
            expression =
                FinishCallExpression(expression);
        }

        return expression;
    }

    private CallExpressionSyntax FinishCallExpression(
        ExpressionSyntax callee)
    {
        Token openParenthesisToken =
            MatchToken(TokenType.LeftParenthesis);

        var arguments =
            new List<ExpressionSyntax>();

        if (Current.Type != TokenType.RightParenthesis &&
            Current.Type != TokenType.EndOfFile)
        {
            while (true)
            {
                arguments.Add(
                    ParseExpression());

                if (Current.Type != TokenType.Comma)
                {
                    break;
                }

                NextToken();
            }
        }

        Token closeParenthesisToken =
            MatchToken(TokenType.RightParenthesis);

        return new CallExpressionSyntax(
            callee,
            openParenthesisToken,
            arguments.ToArray(),
            closeParenthesisToken);
    }

    private ExpressionSyntax ParsePrimaryExpression()
    {
        switch (Current.Type)
        {
            case TokenType.IntegerLiteral:
            case TokenType.FloatLiteral:
            case TokenType.StringLiteral:
            {
                Token literalToken = NextToken();

                return new LiteralExpressionSyntax(
                    literalToken,
                    literalToken.Value);
            }

            case TokenType.TrueKeyword:
            {
                Token literalToken = NextToken();

                return new LiteralExpressionSyntax(
                    literalToken,
                    true);
            }

            case TokenType.FalseKeyword:
            {
                Token literalToken = NextToken();

                return new LiteralExpressionSyntax(
                    literalToken,
                    false);
            }

            case TokenType.NullKeyword:
            {
                Token literalToken = NextToken();

                return new LiteralExpressionSyntax(
                    literalToken,
                    null);
            }

            case TokenType.Identifier:
            {
                Token identifierToken = NextToken();

                return new NameExpressionSyntax(
                    identifierToken);
            }

            case TokenType.LeftParenthesis:
            {
                Token openParenthesisToken =
                    NextToken();

                ExpressionSyntax expression =
                    ParseExpression();

                Token closeParenthesisToken =
                    MatchToken(
                        TokenType.RightParenthesis);

                return new ParenthesizedExpressionSyntax(
                    openParenthesisToken,
                    expression,
                    closeParenthesisToken);
            }

            default:
                return ParseErrorExpression();
        }
    }

    private ErrorExpressionSyntax ParseErrorExpression()
    {
        Token unexpectedToken = Current;

        string foundText =
            unexpectedToken.Type ==
            TokenType.EndOfFile
                ? "end of file"
                : $"`{unexpectedToken.Text}`";

        _diagnostics.Report(
            DiagnosticCodes.ExpectedExpression,
            DiagnosticCategory.Parser,
            $"Expected an expression, but found {foundText}.",
            unexpectedToken.Span);

        bool canConsume =
            unexpectedToken.Type != TokenType.EndOfFile &&
            unexpectedToken.Type != TokenType.Semicolon &&
            unexpectedToken.Type != TokenType.RightParenthesis &&
            unexpectedToken.Type != TokenType.Comma;

        if (canConsume)
        {
            NextToken();
        }

        return new ErrorExpressionSyntax(
            SourceSpan.Empty(
                unexpectedToken.Span.Source,
                unexpectedToken.Span.Start));
    }

    private Token MatchSemicolon()
    {
        if (Current.Type == TokenType.Semicolon)
        {
            return NextToken();
        }

        _diagnostics.Report(
            DiagnosticCodes.ExpectedSemicolon,
            DiagnosticCategory.Parser,
            "Expected `;` after statement.",
            Current.Span,
            "Every CrashScript statement must end with a semicolon.",
            "The statement survived. Its semicolon did not.");

        return CreateMissingToken(
            TokenType.Semicolon,
            Current.Span.Start);
    }

    private Token MatchToken(TokenType expectedType)
    {
        if (Current.Type == expectedType)
        {
            return NextToken();
        }

        string currentText =
            Current.Type == TokenType.EndOfFile
                ? "<eof>"
                : Current.Text;

        _diagnostics.Report(
            DiagnosticCodes.UnexpectedToken,
            DiagnosticCategory.Parser,
            $"Unexpected token `{currentText}`. Expected `{expectedType}`.",
            Current.Span);

        return CreateMissingToken(
            expectedType,
            Current.Span.Start);
    }

    private Token CreateMissingToken(
        TokenType type,
        int position)
    {
        return new Token(
            type,
            string.Empty,
            null,
            SourceSpan.Empty(
                Current.Span.Source,
                position));
    }

    private Token NextToken()
    {
        Token current = Current;
        _position++;

        return current;
    }

    private static bool IsTypeNameToken(
        TokenType type)
    {
        return type is
            TokenType.IntKeyword or
            TokenType.FloatKeyword or
            TokenType.StringKeyword or
            TokenType.BoolKeyword or
            TokenType.VoidKeyword or
            TokenType.Identifier;
    }
}