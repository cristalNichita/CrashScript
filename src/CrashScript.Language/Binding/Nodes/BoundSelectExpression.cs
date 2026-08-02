using CrashScript.Language.Binding.Symbols;
using CrashScript.Language.Source;

namespace CrashScript.Language.Binding.Nodes;

public sealed record BoundSelectExpression(
    IReadOnlyList<BoundSelectBranch> Branches,
    BoundExpression ElseExpression,
    TypeSymbol Type,
    SourceSpan NodeSpan) : BoundExpression(
    Type,
    NodeSpan);