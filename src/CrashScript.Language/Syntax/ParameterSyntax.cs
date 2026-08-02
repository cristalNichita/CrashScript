using CrashScript.Language.Lexing;
using CrashScript.Language.Source;

namespace CrashScript.Language.Syntax;

public sealed record ParameterSyntax(
    Token IdentifierToken,
    Token ColonToken,
    TypeSyntax Type) : SyntaxNode
{
    public string Name => IdentifierToken.Text;
    
    public override SourceSpan Span =>
        SourceSpan.FromBounds(
            IdentifierToken.Span,
            Type.Span);
}