using CrashScript.Language.Lexing;
using CrashScript.Language.Source;

namespace CrashScript.Language.Syntax.Statements;

public sealed record FunctionDeclarationStatementSyntax(
    Token ProcessKeyword,
    Token IdentifierToken,
    Token OpenParenthesisToken,
    IReadOnlyList<ParameterSyntax> Parameters,
    Token CloseParenthesisToken,
    Token ArrowToken,
    TypeSyntax ReturnType,
    BlockStatementSyntax Body,
    Token EndKeyword,
    Token SemicolonToken) : StatementSyntax
{
    public string Name => IdentifierToken.Text;

    public override SourceSpan Span =>
        SourceSpan.FromBounds(
            ProcessKeyword.Span,
            SemicolonToken.Span);
}