using CrashScript.Language.Lexing;
using CrashScript.Language.Source;

namespace CrashScript.Language.Syntax.Expressions;

public sealed record UnaryExpressionSyntax(
    Token OperatorToken,
    ExpressionSyntax Operand) : ExpressionSyntax
{
    public override SourceSpan Span =>
        SourceSpan.FromBounds(
            OperatorToken.Span,
            Operand.Span);
}