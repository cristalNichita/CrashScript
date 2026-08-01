using CrashScript.Language.Lexing;
using CrashScript.Language.Source;

namespace CrashScript.Language.Syntax.Statements;

public sealed record ElseClauseSyntax(
    Token ElseKeyword,
    BlockStatementSyntax Body) : SyntaxNode
{
    public override SourceSpan Span =>
        SourceSpan.FromBounds(
            ElseKeyword.Span,
            Body.Span);
}