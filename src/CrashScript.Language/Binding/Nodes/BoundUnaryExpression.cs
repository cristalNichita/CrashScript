using CrashScript.Language.Binding.Symbols;
using CrashScript.Language.Source;

namespace CrashScript.Language.Binding.Nodes;

public sealed record BoundUnaryExpression(
    BoundUnaryOperatorKind OperatorKind,
    BoundExpression Operand,
    TypeSymbol Type,
    SourceSpan Span) : BoundExpression(Type, Span);