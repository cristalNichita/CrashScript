using CrashScript.Language.Binding.Symbols;
using CrashScript.Language.Source;

namespace CrashScript.Language.Binding.Nodes;

public sealed record BoundBinaryExpression(
    BoundExpression Left,
    BoundBinaryOperatorKind OperatorKind,
    BoundExpression Right,
    TypeSymbol Type,
    SourceSpan Span) : BoundExpression(Type, Span);