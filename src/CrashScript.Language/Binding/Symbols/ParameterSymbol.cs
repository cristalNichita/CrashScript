namespace CrashScript.Language.Binding.Symbols;

public sealed record ParameterSymbol(
    string Name,
    TypeSymbol Type);