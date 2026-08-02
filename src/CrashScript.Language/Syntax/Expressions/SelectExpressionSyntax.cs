using CrashScript.Language.Lexing;
using CrashScript.Language.Source;

namespace CrashScript.Language.Syntax.Expressions;

public sealed record SelectExpressionSyntax(
    Token SelectKeyword,
    IReadOnlyList<SelectBranchSyntax> Branches,
    SelectElseClauseSyntax ElseClause,
    Token EndKeyword) : ExpressionSyntax
{
    public override SourceSpan Span =>
        SourceSpan.FromBounds(
            SelectKeyword.Span,
            EndKeyword.Span);
}