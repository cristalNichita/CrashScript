using CrashScript.Language.Binding.Symbols;
using CrashScript.Language.Source;

namespace CrashScript.Language.Binding.Nodes;

public abstract record BoundExpression(
    TypeSymbol Type,
    SourceSpan Span) : BoundNode(Span);