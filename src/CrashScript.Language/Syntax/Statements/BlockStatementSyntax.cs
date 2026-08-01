using CrashScript.Language.Source;

namespace CrashScript.Language.Syntax.Statements;

public sealed record BlockStatementSyntax(
    IReadOnlyList<StatementSyntax> Statements,
    SourceSpan BlockSpan) : StatementSyntax
{
    public override SourceSpan Span => BlockSpan;
}