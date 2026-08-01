using CrashScript.Language.Lexing;
using CrashScript.Language.Source;

namespace CrashScript.Language.Syntax.Expressions;

public sealed record CallExpressionSyntax(
    ExpressionSyntax Callee,
    Token OpenParenthesisToken,
    IReadOnlyList<ExpressionSyntax> Arguments,
    Token CloseParenthesisToken) : ExpressionSyntax
{
    public override SourceSpan Span =>
        SourceSpan.FromBounds(
            Callee.Span,
            CloseParenthesisToken.Span);
}