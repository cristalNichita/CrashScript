using CrashScript.Language.Binding.Symbols;

namespace CrashScript.Language.Binding;

internal sealed class BoundScope
{
    private readonly Dictionary<string, VariableSymbol>
        _variables = new(StringComparer.Ordinal);

    public BoundScope(BoundScope? parent)
    {
        Parent = parent;
    }

    public BoundScope? Parent { get; }

    public bool TryDeclareVariable(
        VariableSymbol variable)
    {
        ArgumentNullException.ThrowIfNull(variable);

        return _variables.TryAdd(
            variable.Name,
            variable);
    }

    public VariableSymbol? LookupVariable(
        string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (_variables.TryGetValue(
                name,
                out VariableSymbol? variable))
        {
            return variable;
        }

        return Parent?.LookupVariable(name);
    }
}