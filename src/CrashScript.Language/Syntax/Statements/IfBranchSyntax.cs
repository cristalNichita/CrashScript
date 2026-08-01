using CrashScript.Language.Lexing;
using CrashScript.Language.Source;
using CrashScript.Language.Syntax.Expressions;

namespace CrashScript.Language.Syntax.Statements;

public sealed record IfBranchSyntax(
    Token IfKeyword,
    Token? ElseKeyword,
    ExpressionSyntax Condition,
    Token ThenKeyword,
    BlockStatementSyntax Body) : SyntaxNode
{
    public override SourceSpan Span =>
        SourceSpan.FromBounds(
            ElseKeyword?.Span ?? IfKeyword.Span,
            Body.Span);
}