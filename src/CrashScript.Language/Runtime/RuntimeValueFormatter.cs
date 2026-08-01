using System.Globalization;

namespace CrashScript.Language.Runtime;

public static class RuntimeValueFormatter
{
    public static string Format(object? value)
    {
        return value switch
        {
            null => "null",
            
            bool boolean =>
                boolean ? "true" : "false",
            
            long integer =>
                integer.ToString(
                    CultureInfo.InvariantCulture),
            
            double floatingPoint =>
                floatingPoint.ToString(
                    "G",
                    CultureInfo.InvariantCulture),
            
            string text => text,
            
            IFormattable formattable =>
                formattable.ToString(
                    null,
                    CultureInfo.InvariantCulture),
            
            _ => value.ToString() ?? "null"
        };
    }
}