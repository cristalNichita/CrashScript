using CrashScript.Language.Lexing;
using CrashScript.Language.Source;
using CrashScript.Language.Syntax.Expressions;

namespace CrashScript.Language.Syntax.Statements;

public sealed record GuardStatementSyntax(
    Token GuardKeyword,
    ExpressionSyntax Condition,
    Token ElseKeyword,
    ExpressionSyntax Message,
    Token SemicolonToken) : StatementSyntax
{
    public override SourceSpan Span =>
        SourceSpan.FromBounds(
            GuardKeyword.Span,
            SemicolonToken.Span);
}