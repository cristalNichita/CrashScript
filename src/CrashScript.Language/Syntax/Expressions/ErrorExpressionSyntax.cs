using CrashScript.Language.Source;

namespace CrashScript.Language.Syntax.Expressions;

/// <summary>
/// Placeholder node created when the parser cannot produce
/// a valid expression but needs to continue parsing.
/// </summary>
public sealed record ErrorExpressionSyntax(
    SourceSpan ErrorSpan) : ExpressionSyntax
{
    public override SourceSpan Span => ErrorSpan;
}