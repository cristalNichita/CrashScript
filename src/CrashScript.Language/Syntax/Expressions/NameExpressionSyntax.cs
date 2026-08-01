using CrashScript.Language.Lexing;
using CrashScript.Language.Source;

namespace CrashScript.Language.Syntax.Expressions;

public sealed record NameExpressionSyntax(
    Token IdentifierToken) : ExpressionSyntax
{
    public string Name => IdentifierToken.Text;
    
    public override SourceSpan Span => IdentifierToken.Span;
}