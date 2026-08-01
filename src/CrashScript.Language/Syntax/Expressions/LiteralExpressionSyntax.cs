using CrashScript.Language.Lexing;
using CrashScript.Language.Source;

namespace CrashScript.Language.Syntax.Expressions;

public sealed record LiteralExpressionSyntax(
    Token LiteralToken,
    object? Value) : ExpressionSyntax
{
    public override SourceSpan Span => LiteralToken.Span;
}