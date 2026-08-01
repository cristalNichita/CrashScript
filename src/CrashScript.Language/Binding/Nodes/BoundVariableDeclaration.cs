using CrashScript.Language.Binding.Symbols;
using CrashScript.Language.Source;

namespace CrashScript.Language.Binding.Nodes;

public sealed record BoundVariableDeclaration(
    VariableSymbol Variable,
    BoundExpression Initializer,
    SourceSpan Span) : BoundStatement(Span);