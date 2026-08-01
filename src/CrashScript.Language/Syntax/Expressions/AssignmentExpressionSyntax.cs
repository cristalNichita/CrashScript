using CrashScript.Language.Lexing;
using CrashScript.Language.Source;

namespace CrashScript.Language.Syntax.Expressions;

public sealed record AssignmentExpressionSyntax(
    ExpressionSyntax Target,
    Token EqualsToken,
    ExpressionSyntax Value) : ExpressionSyntax
{
    public override SourceSpan Span =>
        SourceSpan.FromBounds(
            Target.Span,
            Value.Span);
}