using CrashScript.Language.Lexing;
using CrashScript.Language.Source;

namespace CrashScript.Language.Syntax.Statements;

public sealed record IfStatementSyntax(
    IReadOnlyList<IfBranchSyntax> Branches,
    ElseClauseSyntax? ElseClause,
    Token EndKeyword,
    Token SemicolonToken) : StatementSyntax
{
    public override SourceSpan Span =>
        SourceSpan.FromBounds(
            Branches[0].Span,
            SemicolonToken.Span);
}