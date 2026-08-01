using CrashScript.Language.Binding.Symbols;

namespace CrashScript.Language.Runtime;

public sealed class RuntimeEnvironment
{
    private readonly Dictionary<VariableSymbol, object?>
        _values = [];

    public RuntimeEnvironment(
        RuntimeEnvironment? parent = null)
    {
        Parent = parent;
    }

    public RuntimeEnvironment? Parent { get; }

    public void Define(
        VariableSymbol variable,
        object? value)
    {
        ArgumentNullException.ThrowIfNull(variable);

        if (!_values.TryAdd(variable, value))
        {
            throw new InvalidOperationException(
                $"Variable `{variable.Name}` was already defined in this runtime environment.");
        }
    }

    public object? Get(VariableSymbol variable)
    {
        ArgumentNullException.ThrowIfNull(variable);

        if (_values.TryGetValue(
                variable,
                out object? value))
        {
            return value;
        }

        if (Parent is not null)
        {
            return Parent.Get(variable);
        }

        throw new InvalidOperationException(
            $"Runtime value for `{variable.Name}` was not found.");
    }

    public void Assign(
        VariableSymbol variable,
        object? value)
    {
        ArgumentNullException.ThrowIfNull(variable);

        if (_values.ContainsKey(variable))
        {
            _values[variable] = value;
            return;
        }

        if (Parent is not null)
        {
            Parent.Assign(variable, value);
            return;
        }

        throw new InvalidOperationException(
            $"Runtime value for `{variable.Name}` was not found.");
    }
}