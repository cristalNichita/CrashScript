namespace CrashScript.Language.Binding.Symbols;

public sealed class VariableSymbol
{
    public VariableSymbol(
        string name,
        TypeSymbol type,
        bool isReadOnly)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(type);

        Name = name;
        Type = type;
        IsReadOnly = isReadOnly;
    }

    public string Name { get; }

    public TypeSymbol Type { get; }

    public bool IsReadOnly { get; }

    public override string ToString()
    {
        string prefix =
            IsReadOnly ? "fixed" : "memory";

        return $"{prefix} {Name}: {Type.Name}";
    }
}