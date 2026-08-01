namespace CrashScript.Language.Binding.Symbols;

public static class BuiltinFunctions
{
    public static FunctionSymbol Log { get; } =
        Create(
            "log",
            TypeSymbol.Void,
            ("value", TypeSymbol.Any));

    public static FunctionSymbol Input { get; } =
        Create(
            "input",
            TypeSymbol.String,
            ("prompt", TypeSymbol.String));

    public static FunctionSymbol Length { get; } =
        Create(
            "length",
            TypeSymbol.Int,
            ("value", TypeSymbol.String));

    public static FunctionSymbol ToIntFromString { get; } =
        Create(
            "toInt",
            TypeSymbol.Nullable(TypeSymbol.Int),
            ("value", TypeSymbol.String));

    public static FunctionSymbol ToIntFromFloat { get; } =
        Create(
            "toInt",
            TypeSymbol.Int,
            ("value", TypeSymbol.Float));

    public static FunctionSymbol ToFloatFromString { get; } =
        Create(
            "toFloat",
            TypeSymbol.Nullable(TypeSymbol.Float),
            ("value", TypeSymbol.String));

    public static FunctionSymbol ToFloatFromInt { get; } =
        Create(
            "toFloat",
            TypeSymbol.Float,
            ("value", TypeSymbol.Int));

    public new static FunctionSymbol ToString { get; } =
        Create(
            "toString",
            TypeSymbol.String,
            ("value", TypeSymbol.Any));

    public static FunctionSymbol RandomFloat { get; } =
        Create(
            "random",
            TypeSymbol.Float);

    public static FunctionSymbol RandomInt { get; } =
        Create(
            "random",
            TypeSymbol.Int,
            ("min", TypeSymbol.Int),
            ("max", TypeSymbol.Int));

    private static readonly FunctionSymbol[] Functions =
    [
        Log,
        Input,
        Length,
        ToIntFromString,
        ToIntFromFloat,
        ToFloatFromString,
        ToFloatFromInt,
        ToString,
        RandomFloat,
        RandomInt
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