using System.Globalization;
using System.Text;
using CrashScript.Language.Diagnostics;
using CrashScript.Language.Source;

namespace CrashScript.Language.Lexing;

public sealed class Lexer
{
    private static readonly IReadOnlyDictionary<string, TokenType> Keywords =
        new Dictionary<string, TokenType>(StringComparer.Ordinal)
        {
            ["memory"] = TokenType.MemoryKeyword,
            ["fixed"] = TokenType.FixedKeyword,
            ["process"] = TokenType.ProcessKeyword,
            ["return"] = TokenType.ReturnKeyword,

            ["if"] = TokenType.IfKeyword,
            ["else"] = TokenType.ElseKeyword,
            ["while"] = TokenType.WhileKeyword,
            ["end"] = TokenType.EndKeyword,

            ["and"] = TokenType.AndKeyword,
            ["or"] = TokenType.OrKeyword,
            ["not"] = TokenType.NotKeyword,

            ["select"] = TokenType.SelectKeyword,
            ["when"] = TokenType.WhenKeyword,
            ["guard"] = TokenType.GuardKeyword,

            ["true"] = TokenType.TrueKeyword,
            ["false"] = TokenType.FalseKeyword,
            ["null"] = TokenType.NullKeyword,

            ["int"] = TokenType.IntKeyword,
            ["float"] = TokenType.FloatKeyword,
            ["string"] = TokenType.StringKeyword,
            ["bool"] = TokenType.BoolKeyword,
            ["void"] = TokenType.VoidKeyword
        };

    private readonly SourceText _source;
    private readonly DiagnosticBag _diagnostics = new();

    private int _position;

    public Lexer(SourceText source)
    {
        ArgumentNullException.ThrowIfNull(source);

        _source = source;
    }

    public LexResult Lex()
    {
        var tokens = new List<Token>();

        while (true)
        {
            Token token = NextToken();
            tokens.Add(token);

            if (token.Type == TokenType.EndOfFile)
            {
                break;
            }
        }

        return new LexResult(
            tokens.ToArray(),
            _diagnostics.ToArray());
    }

    private char Current => Peek(0);

    private char Peek(int offset)
    {
        int index = _position + offset;

        if (index >= _source.Length)
        {
            return '\0';
        }

        return _source[index];
    }

    private Token NextToken()
    {
        SkipTrivia();

        int tokenStart = _position;

        if (Current == '\0')
        {
            return new Token(
                TokenType.EndOfFile,
                string.Empty,
                null,
                SourceSpan.Empty(_source, _position));
        }

        if (char.IsLetter(Current) || Current == '_')
        {
            return ReadIdentifierOrKeyword();
        }

        if (char.IsDigit(Current))
        {
            return ReadNumber();
        }

        return Current switch
        {
            '"' => ReadString(),

            '(' => ReadFixedToken(TokenType.LeftParenthesis, 1),
            ')' => ReadFixedToken(TokenType.RightParenthesis, 1),
            ',' => ReadFixedToken(TokenType.Comma, 1),
            ';' => ReadFixedToken(TokenType.Semicolon, 1),

            '+' => ReadFixedToken(TokenType.Plus, 1),
            '*' => ReadFixedToken(TokenType.Star, 1),
            '/' => ReadFixedToken(TokenType.Slash, 1),
            '%' => ReadFixedToken(TokenType.Percent, 1),

            ':' when Peek(1) == '=' =>
                ReadFixedToken(TokenType.ColonEqual, 2),

            ':' => ReadFixedToken(TokenType.Colon, 1),

            '-' when Peek(1) == '>' =>
                ReadFixedToken(TokenType.Arrow, 2),

            '-' => ReadFixedToken(TokenType.Minus, 1),

            '=' when Peek(1) == '=' =>
                ReadFixedToken(TokenType.EqualEqual, 2),

            '=' when Peek(1) == '>' =>
                ReadFixedToken(TokenType.FatArrow, 2),

            '=' => ReadFixedToken(TokenType.Equal, 1),

            '!' when Peek(1) == '=' =>
                ReadFixedToken(TokenType.BangEqual, 2),

            '>' when Peek(1) == '=' =>
                ReadFixedToken(TokenType.GreaterEqual, 2),

            '>' => ReadFixedToken(TokenType.Greater, 1),

            '<' when Peek(1) == '=' =>
                ReadFixedToken(TokenType.LessEqual, 2),

            '<' => ReadFixedToken(TokenType.Less, 1),

            '?' when Peek(1) == '?' =>
                ReadFixedToken(TokenType.QuestionQuestion, 2),

            '?' => ReadFixedToken(TokenType.Question, 1),

            _ => ReadUnexpectedCharacter(tokenStart)
        };
    }

    private void SkipTrivia()
    {
        while (true)
        {
            if (char.IsWhiteSpace(Current))
            {
                _position++;
                continue;
            }

            if (Current == '/' && Peek(1) == '/')
            {
                SkipLineComment();
                continue;
            }

            break;
        }
    }

    private void SkipLineComment()
    {
        _position += 2;

        while (Current != '\0' &&
               Current != '\r' &&
               Current != '\n')
        {
            _position++;
        }
    }

    private Token ReadIdentifierOrKeyword()
    {
        int start = _position;

        while (char.IsLetterOrDigit(Current) || Current == '_')
        {
            _position++;
        }

        string text = _source.Text.Substring(
            start,
            _position - start);

        TokenType type = Keywords.TryGetValue(text, out TokenType keywordType)
            ? keywordType
            : TokenType.Identifier;

        return new Token(
            type,
            text,
            null,
            new SourceSpan(_source, start, _position - start));
    }

    private Token ReadNumber()
    {
        int start = _position;

        while (char.IsDigit(Current))
        {
            _position++;
        }

        bool isFloat = false;

        if (Current == '.' && char.IsDigit(Peek(1)))
        {
            isFloat = true;
            _position++;

            while (char.IsDigit(Current))
            {
                _position++;
            }
        }

        int length = _position - start;
        string text = _source.Text.Substring(start, length);
        var span = new SourceSpan(_source, start, length);

        if (isFloat)
        {
            if (!double.TryParse(
                    text,
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out double floatValue))
            {
                _diagnostics.Report(
                    DiagnosticCodes.InvalidNumber,
                    DiagnosticCategory.Lexer,
                    $"Invalid floating-point literal `{text}`.",
                    span);

                floatValue = 0;
            }

            return new Token(
                TokenType.FloatLiteral,
                text,
                floatValue,
                span);
        }

        if (!long.TryParse(
                text,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long integerValue))
        {
            _diagnostics.Report(
                DiagnosticCodes.InvalidNumber,
                DiagnosticCategory.Lexer,
                $"Integer literal `{text}` is outside the supported range.",
                span,
                "CrashScript integers currently use signed 64-bit values.");

            integerValue = 0;
        }

        return new Token(
            TokenType.IntegerLiteral,
            text,
            integerValue,
            span);
    }

    private Token ReadString()
    {
        int start = _position;
        var valueBuilder = new StringBuilder();

        // Opening quote
        _position++;

        while (true)
        {
            if (Current == '\0' ||
                Current == '\r' ||
                Current == '\n')
            {
                int unterminatedLength = _position - start;

                _diagnostics.Report(
                    DiagnosticCodes.UnterminatedString,
                    DiagnosticCategory.Lexer,
                    "Unterminated string literal.",
                    new SourceSpan(
                        _source,
                        start,
                        unterminatedLength),
                    "Add a closing double quote before the end of the line.",
                    "The string kept going. Its motivation did not.");

                return new Token(
                    TokenType.StringLiteral,
                    _source.Text.Substring(start, unterminatedLength),
                    valueBuilder.ToString(),
                    new SourceSpan(
                        _source,
                        start,
                        unterminatedLength));
            }

            if (Current == '"')
            {
                _position++;

                int length = _position - start;

                return new Token(
                    TokenType.StringLiteral,
                    _source.Text.Substring(start, length),
                    valueBuilder.ToString(),
                    new SourceSpan(_source, start, length));
            }

            if (Current == '\\')
            {
                ReadEscapeSequence(valueBuilder);
                continue;
            }

            valueBuilder.Append(Current);
            _position++;
        }
    }

    private void ReadEscapeSequence(StringBuilder valueBuilder)
    {
        int escapeStart = _position;

        // Skip backslash.
        _position++;

        char escapedCharacter = Current;

        switch (escapedCharacter)
        {
            case 'n':
                valueBuilder.Append('\n');
                _position++;
                return;

            case 'r':
                valueBuilder.Append('\r');
                _position++;
                return;

            case 't':
                valueBuilder.Append('\t');
                _position++;
                return;

            case '"':
                valueBuilder.Append('"');
                _position++;
                return;

            case '\\':
                valueBuilder.Append('\\');
                _position++;
                return;

            case '\0':
            case '\r':
            case '\n':
                return;

            default:
                int escapeLength = Math.Min(
                    2,
                    _source.Length - escapeStart);

                var span = new SourceSpan(
                    _source,
                    escapeStart,
                    escapeLength);

                _diagnostics.Report(
                    DiagnosticCodes.InvalidEscapeSequence,
                    DiagnosticCategory.Lexer,
                    $"Invalid escape sequence `\\{escapedCharacter}`.",
                    span,
                    "Supported escapes are \\n, \\r, \\t, \\\", and \\\\.");

                valueBuilder.Append(escapedCharacter);
                _position++;
                return;
        }
    }

    private Token ReadFixedToken(TokenType type, int length)
    {
        int start = _position;
        _position += length;

        return new Token(
            type,
            _source.Text.Substring(start, length),
            null,
            new SourceSpan(_source, start, length));
    }

    private Token ReadUnexpectedCharacter(int start)
    {
        char character = Current;
        _position++;

        var span = new SourceSpan(_source, start, 1);

        string? hint = character == '!'
            ? "CrashScript uses `not` for logical negation and `!=` for inequality."
            : null;

        _diagnostics.Report(
            DiagnosticCodes.UnexpectedCharacter,
            DiagnosticCategory.Lexer,
            $"Unexpected character `{FormatCharacter(character)}`.",
            span,
            hint);

        return new Token(
            TokenType.BadToken,
            character.ToString(),
            null,
            span);
    }

    private static string FormatCharacter(char character)
    {
        return character switch
        {
            '\r' => "\\r",
            '\n' => "\\n",
            '\t' => "\\t",
            _ => character.ToString()
        };
    }
}