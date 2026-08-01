using CrashScript.Language.Lexing;
using CrashScript.Language.Source;

namespace CrashScript.Language.Syntax.Expressions;

public sealed record ParenthesizedExpressionSyntax(
    Token OpenParenthesisToken,
    ExpressionSyntax Expression,
    Token CloseParenthesisToken) : ExpressionSyntax
{
    public override SourceSpan Span =>
        SourceSpan.FromBounds(
            OpenParenthesisToken.Span,
            CloseParenthesisToken.Span);
}