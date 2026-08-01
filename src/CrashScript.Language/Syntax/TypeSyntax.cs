using CrashScript.Language.Lexing;
using CrashScript.Language.Source;

namespace CrashScript.Language.Syntax;

public sealed record TypeSyntax(
    Token NameToken,
    Token? QuestionToken) : SyntaxNode
{
    public string Name => NameToken.Text;

    public bool IsNullable => QuestionToken is not null;
    
    public override SourceSpan Span =>
        QuestionToken is null
            ? NameToken.Span
            : SourceSpan.FromBounds(
                NameToken.Span,
                QuestionToken.Span);
}