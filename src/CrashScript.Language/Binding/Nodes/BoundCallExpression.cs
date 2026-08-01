using CrashScript.Language.Binding.Symbols;
using CrashScript.Language.Source;

namespace CrashScript.Language.Binding.Nodes;

public sealed record BoundCallExpression(
    FunctionSymbol Function,
    IReadOnlyList<BoundExpression> Arguments,
    TypeSymbol Type,
    SourceSpan Span) : BoundExpression(Type, Span);