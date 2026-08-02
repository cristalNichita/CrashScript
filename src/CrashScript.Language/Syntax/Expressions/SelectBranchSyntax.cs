using CrashScript.Language.Lexing;
using CrashScript.Language.Source;

namespace CrashScript.Language.Syntax.Expressions;

public sealed record SelectBranchSyntax(
    Token WhenKeyword,
    ExpressionSyntax Condition,
    Token FatArrowToken,
    ExpressionSyntax Value,
    Token SemicolonToken) : SyntaxNode
{
    public override SourceSpan Span =>
        SourceSpan.FromBounds(
            WhenKeyword.Span,
            SemicolonToken.Span);
}