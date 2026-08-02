namespace CrashScript.Language.Binding.Symbols;

public sealed class ParameterSymbol : VariableSymbol
{
    public ParameterSymbol(
        string name,
        TypeSymbol type)
        : base(
            name,
            type,
            isReadOnly: false)
    {
    }
}