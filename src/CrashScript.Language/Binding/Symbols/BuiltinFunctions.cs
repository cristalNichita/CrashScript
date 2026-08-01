namespace CrashScript.Language.Binding.Symbols;

public static class BuiltinFunctions
{
    private static readonly FunctionSymbol[] Functions =
    [
        Create(
            "log",
            TypeSymbol.Void,
            ("value", TypeSymbol.Any)),

        Create(
            "input",
            TypeSymbol.String,
            ("prompt", TypeSymbol.String)),

        Create(
            "length",
            TypeSymbol.Int,
            ("value", TypeSymbol.String)),

        Create(
            "toInt",
            TypeSymbol.Nullable(TypeSymbol.Int),
            ("value", TypeSymbol.String)),

        Create(
            "toInt",
            TypeSymbol.Int,
            ("value", TypeSymbol.Float)),

        Create(
            "toFloat",
            TypeSymbol.Nullable(TypeSymbol.Float),
            ("value", TypeSymbol.String)),

        Create(
            "toFloat",
            TypeSymbol.Float,
            ("value", TypeSymbol.Int)),

        Create(
            "toString",
            TypeSymbol.String,
            ("value", TypeSymbol.Any)),

        Create(
            "random",
            TypeSymbol.Float),

        Create(
            "random",
            TypeSymbol.Int,
            ("min", TypeSymbol.Int),
            ("max", TypeSymbol.Int))
    ];

    public static IReadOnlyList<FunctionSymbol> All => Functions;

    public static IReadOnlyList<FunctionSymbol> Find(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return Functions
            .Where(function =>
                string.Equals(
                    function.Name,
                    name,
                    StringComparison.Ordinal))
            .ToArray();
    }

    private static FunctionSymbol Create(
        string name,
        TypeSymbol returnType,
        params (string Name, TypeSymbol Type)[] parameters)
    {
        ParameterSymbol[] parameterSymbols = parameters
            .Select(parameter =>
                new ParameterSymbol(
                    parameter.Name,
                    parameter.Type))
            .ToArray();

        return new FunctionSymbol(
            name,
            parameterSymbols,
            returnType);
    }
}