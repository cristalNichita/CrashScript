namespace CrashScript.Language.Binding.Symbols;

public sealed class FunctionSymbol
{
    public FunctionSymbol(
        string name,
        IReadOnlyList<ParameterSymbol> parameters,
        TypeSymbol returnType,
        bool isNative)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(returnType);

        Name = name;
        Parameters = parameters;
        ReturnType = returnType;
        IsNative = isNative;
    }

    public string Name { get; }

    public IReadOnlyList<ParameterSymbol> Parameters { get; }

    public TypeSymbol ReturnType { get; }

    public bool IsNative { get; }

    public string Signature
    {
        get
        {
            string parameters = string.Join(
                ", ",
                Parameters.Select(parameter =>
                    $"{parameter.Name}: {parameter.Type.Name}"));

            return $"{Name}({parameters}) -> {ReturnType.Name}";
        }
    }

    public override string ToString()
    {
        return Signature;
    }
}