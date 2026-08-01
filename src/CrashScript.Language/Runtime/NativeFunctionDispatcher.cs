using System.Globalization;
using CrashScript.Language.Binding.Symbols;
using CrashScript.Language.Diagnostics;
using CrashScript.Language.Source;

namespace CrashScript.Language.Runtime;

public sealed class NativeFunctionDispatcher
{
    private readonly ICrashConsole _console;
    private readonly Random _random;

    public NativeFunctionDispatcher(
        ICrashConsole console,
        Random random)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(random);

        _console = console;
        _random = random;
    }

    public object? Invoke(
        FunctionSymbol function,
        IReadOnlyList<object?> arguments,
        SourceSpan callSpan)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(arguments);

        if (ReferenceEquals(
                function,
                BuiltinFunctions.Log))
        {
            string text = RuntimeValueFormatter.Format(
                arguments[0]);

            _console.WriteLine(text);

            return null;
        }

        if (ReferenceEquals(
                function,
                BuiltinFunctions.Input))
        {
            string prompt = (string)arguments[0]!;

            _console.Write(prompt);

            return _console.ReadLine() ?? string.Empty;
        }

        if (ReferenceEquals(
                function,
                BuiltinFunctions.Length))
        {
            string value = (string)arguments[0]!;

            return (long)value.Length;
        }

        if (ReferenceEquals(
                function,
                BuiltinFunctions.ToIntFromString))
        {
            string value = (string)arguments[0]!;

            return long.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out long result)
                ? result
                : null;
        }

        if (ReferenceEquals(
                function,
                BuiltinFunctions.ToIntFromFloat))
        {
            double value = (double)arguments[0]!;

            return ConvertFloatToInteger(
                value,
                callSpan);
        }

        if (ReferenceEquals(
                function,
                BuiltinFunctions.ToFloatFromString))
        {
            string value = (string)arguments[0]!;

            return double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double result)
                ? result
                : null;
        }

        if (ReferenceEquals(
                function,
                BuiltinFunctions.ToFloatFromInt))
        {
            long value = (long)arguments[0]!;

            return (double)value;
        }

        if (ReferenceEquals(
                function,
                BuiltinFunctions.ToString))
        {
            return RuntimeValueFormatter.Format(
                arguments[0]);
        }

        if (ReferenceEquals(
                function,
                BuiltinFunctions.RandomFloat))
        {
            return _random.NextDouble();
        }

        if (ReferenceEquals(
                function,
                BuiltinFunctions.RandomInt))
        {
            long minimum = (long)arguments[0]!;
            long maximum = (long)arguments[1]!;

            return NextIntegerInclusive(
                minimum,
                maximum,
                callSpan);
        }

        throw new InvalidOperationException(
            $"No runtime implementation exists for `{function.Signature}`.");
    }

    private static long ConvertFloatToInteger(
        double value,
        SourceSpan span)
    {
        if (double.IsNaN(value) ||
            double.IsInfinity(value) ||
            value < long.MinValue ||
            value > long.MaxValue)
        {
            throw new RuntimeFault(
                DiagnosticCodes.NumericOverflow,
                $"Value `{RuntimeValueFormatter.Format(value)}` cannot be converted to `int`.",
                span,
                "The value must fit inside a signed 64-bit integer.");
        }

        double truncated = Math.Truncate(value);

        return checked((long)truncated);
    }

    private long NextIntegerInclusive(
        long minimum,
        long maximum,
        SourceSpan span)
    {
        if (minimum > maximum)
        {
            throw new RuntimeFault(
                DiagnosticCodes.InvalidRandomRange,
                $"Invalid random range: minimum `{minimum}` is greater than maximum `{maximum}`.",
                span,
                "The first argument of `random(min, max)` must not exceed the second.",
                "Randomness still requires some rules.");
        }

        if (minimum == maximum)
        {
            return minimum;
        }

        // Normal case. NextInt64 excludes the upper bound,
        // so we add one to make CrashScript's maximum inclusive.
        if (maximum < long.MaxValue)
        {
            return _random.NextInt64(
                minimum,
                maximum + 1);
        }

        // Avoid overflowing maximum + 1.
        if (minimum > long.MinValue)
        {
            return _random.NextInt64(
                       minimum - 1,
                       maximum) +
                   1;
        }

        // Complete signed 64-bit range.
        Span<byte> bytes = stackalloc byte[sizeof(long)];
        _random.NextBytes(bytes);

        return BitConverter.ToInt64(bytes);
    }
}