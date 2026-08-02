using CrashScript.Language.Lexing;
using CrashScript.Language.Source;

namespace CrashScript.Language.Syntax.Expressions;

public sealed record SelectElseClauseSyntax(
    Token ElseKeyword,
    Token FatArrowToken,
    ExpressionSyntax Value,
    Token SemicolonToken) : SyntaxNode
{
    public override SourceSpan Span =>
        SourceSpan.FromBounds(
            ElseKeyword.Span,
            SemicolonToken.Span);
}