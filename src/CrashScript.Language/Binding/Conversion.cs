using CrashScript.Language.Binding.Symbols;

namespace CrashScript.Language.Binding;

public readonly record struct Conversion(
    bool Exists,
    bool IsIdentity,
    int Cost)
{
    public static Conversion None { get; } =
        new(false, false, int.MaxValue);

    public static Conversion Identity { get; } =
        new(true, true, 0);

    public static Conversion Implicit(int cost = 1)
    {
        return new Conversion(
            true,
            false,
            cost);
    }

    public static Conversion Classify(
        TypeSymbol source,
        TypeSymbol target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        if (source == TypeSymbol.Error ||
            target == TypeSymbol.Error)
        {
            return Identity;
        }

        if (source == target)
        {
            return Identity;
        }

        // Native functions such as log(any).
        if (target == TypeSymbol.Any)
        {
            return Implicit(10);
        }

        // Safe numeric widening.
        if (source == TypeSymbol.Int &&
            target == TypeSymbol.Float)
        {
            return Implicit();
        }

        if (target is NullableTypeSymbol nullableTarget)
        {
            if (source == TypeSymbol.Null)
            {
                return Implicit();
            }

            Conversion underlyingConversion = Classify(
                source,
                nullableTarget.UnderlyingType);

            if (underlyingConversion.Exists)
            {
                return Implicit(
                    underlyingConversion.Cost + 1);
            }
        }

        return None;
    }
}