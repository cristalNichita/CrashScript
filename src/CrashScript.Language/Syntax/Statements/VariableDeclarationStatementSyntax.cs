using CrashScript.Language.Lexing;
using CrashScript.Language.Source;
using CrashScript.Language.Syntax.Expressions;

namespace CrashScript.Language.Syntax.Statements;

public sealed record VariableDeclarationStatementSyntax(
    Token DeclarationKeyword,
    Token IdentifierToken,
    Token? ColonToken,
    TypeSyntax? DeclaredType,
    Token AssignmentToken,
    ExpressionSyntax Initializer,
    Token SemicolonToken) : StatementSyntax
{
    public string Name => IdentifierToken.Text;

    public bool IsFixed =>
        DeclarationKeyword.Type == TokenType.FixedKeyword;

    public bool UsesTypeInference =>
        AssignmentToken.Type == TokenType.ColonEqual;

    public override SourceSpan Span =>
        SourceSpan.FromBounds(
            DeclarationKeyword.Span,
            SemicolonToken.Span);
}