using CrashScript.Language.Lexing;
using CrashScript.Language.Source;

namespace CrashScript.Language.Syntax.Expressions;

public sealed record BinaryExpressionSyntax(
    ExpressionSyntax Left,
    Token OperatorToken,
    ExpressionSyntax Right) : ExpressionSyntax
{
    public override SourceSpan Span =>
        SourceSpan.FromBounds(
            Left.Span,
            Right.Span);
}