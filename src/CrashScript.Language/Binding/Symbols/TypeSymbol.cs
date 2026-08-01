namespace CrashScript.Language.Binding.Symbols;

public abstract record TypeSymbol
{
    protected TypeSymbol(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public virtual bool IsNullable => false;

    public static PrimitiveTypeSymbol Int { get; } = new("int");

    public static PrimitiveTypeSymbol Float { get; } = new("float");

    public static PrimitiveTypeSymbol String { get; } = new("string");

    public static PrimitiveTypeSymbol Bool { get; } = new("bool");

    public static PrimitiveTypeSymbol Void { get; } = new("void");

    /// <summary>
    /// Represents the null literal before it is converted
    /// to a concrete nullable type.
    /// </summary>
    public static PrimitiveTypeSymbol Null { get; } = new("null");

    /// <summary>
    /// Used only by native function signatures such as log(any).
    /// CrashScript users cannot declare variables of this type.
    /// </summary>
    public static PrimitiveTypeSymbol Any { get; } = new("any");

    /// <summary>
    /// Prevents one type error from creating many cascading errors.
    /// </summary>
    public static PrimitiveTypeSymbol Error { get; } = new("<error>");

    public static TypeSymbol Nullable(TypeSymbol underlyingType)
    {
        ArgumentNullException.ThrowIfNull(underlyingType);

        if (underlyingType is NullableTypeSymbol)
        {
            return underlyingType;
        }

        if (underlyingType == Void ||
            underlyingType == Any ||
            underlyingType == Error ||
            underlyingType == Null)
        {
            throw new ArgumentException(
                $"Type `{underlyingType.Name}` cannot be nullable.",
                nameof(underlyingType));
        }

        return new NullableTypeSymbol(underlyingType);
    }

    public override string ToString()
    {
        return Name;
    }
}

public sealed record PrimitiveTypeSymbol : TypeSymbol
{
    public PrimitiveTypeSymbol(string name)
        : base(name)
    {
    }
}

public sealed record NullableTypeSymbol : TypeSymbol
{
    public NullableTypeSymbol(TypeSymbol underlyingType)
        : base($"{underlyingType.Name}?")
    {
        ArgumentNullException.ThrowIfNull(underlyingType);

        UnderlyingType = underlyingType;
    }

    public TypeSymbol UnderlyingType { get; }

    public override bool IsNullable => true;
}