using CrashScript.Language.Lexing;
using CrashScript.Language.Source;
using CrashScript.Language.Syntax.Expressions;

namespace CrashScript.Language.Syntax.Statements;

public sealed record ReturnStatementSyntax(
    Token ReturnKeyword,
    ExpressionSyntax? Expression,
    Token SemicolonToken) : StatementSyntax
{
    public override SourceSpan Span =>
        SourceSpan.FromBounds(
            ReturnKeyword.Span,
            SemicolonToken.Span);
}