using CrashScript.Language.Lexing;
using CrashScript.Language.Source;
using CrashScript.Language.Syntax.Expressions;

namespace CrashScript.Language.Syntax.Statements;

public sealed record WhileStatementSyntax(
    Token WhileKeyword,
    ExpressionSyntax Condition,
    Token DoKeyword,
    BlockStatementSyntax Body,
    Token EndKeyword,
    Token SemicolonToken) : StatementSyntax
{
    public override SourceSpan Span =>
        SourceSpan.FromBounds(
            WhileKeyword.Span,
            SemicolonToken.Span);
}