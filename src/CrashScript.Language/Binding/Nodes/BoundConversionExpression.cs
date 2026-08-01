using CrashScript.Language.Binding.Symbols;
using CrashScript.Language.Source;

namespace CrashScript.Language.Binding.Nodes;

public sealed record BoundConversionExpression(
    BoundExpression Expression,
    TypeSymbol Type,
    SourceSpan Span) : BoundExpression(Type, Span);