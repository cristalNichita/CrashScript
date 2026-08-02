using CrashScript.Language.Binding.Symbols;
using CrashScript.Language.Source;

namespace CrashScript.Language.Binding.Nodes;

public sealed record BoundFunctionDeclaration(
    FunctionSymbol Function,
    BoundBlockStatement Body,
    SourceSpan NodeSpan) : BoundStatement(NodeSpan);