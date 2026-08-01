namespace CrashScript.Language.Binding.Symbols;

public sealed record FunctionSymbol(
    string Name,
    IReadOnlyList<ParameterSymbol> Parameters,
    TypeSymbol ReturnType)
{
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