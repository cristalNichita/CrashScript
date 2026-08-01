using CrashScript.Language.Binding.Symbols;
using CrashScript.Language.Source;

namespace CrashScript.Language.Binding.Nodes;

public sealed record BoundVariableExpression(
    VariableSymbol Variable,
    SourceSpan Span) : BoundExpression(
        Variable.Type,
        Span);