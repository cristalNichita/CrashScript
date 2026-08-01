using CrashScript.Language.Lexing;
using CrashScript.Language.Source;
using CrashScript.Language.Syntax.Statements;

namespace CrashScript.Language.Syntax;

public sealed record CompilationUnitSyntax(
    IReadOnlyList<StatementSyntax> Statements,
    Token EndOfFileToken) : SyntaxNode
{
    public override SourceSpan Span
    {
        get
        {
            if (Statements.Count == 0)
            {
                return EndOfFileToken.Span;
            }
            
            return SourceSpan.FromBounds(
                Statements[0].Span,
                EndOfFileToken.Span);
        }
    }
}