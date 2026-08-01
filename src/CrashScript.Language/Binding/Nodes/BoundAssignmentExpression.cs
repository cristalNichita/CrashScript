using CrashScript.Language.Binding.Symbols;
using CrashScript.Language.Source;

namespace CrashScript.Language.Binding.Nodes;

public sealed record BoundAssignmentExpression(
    VariableSymbol Variable,
    BoundExpression Expression,
    SourceSpan Span) : BoundExpression(
        Variable.Type,
        Span);