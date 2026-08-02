namespace CrashScript.Cli;

public static class HelpText
{
    public static void Write()
    {
        Console.WriteLine(
            $"{AppInfo.Name} {AppInfo.Version}");

        Console.WriteLine(
            "A statically typed interpreted programming language.");
        Console.WriteLine();

        Console.WriteLine("Usage:");
        Console.WriteLine(
            "  crashscript <file.crash>");
        Console.WriteLine(
            "  crashscript --check <file.crash>");
        Console.WriteLine(
            "  crashscript --tokens <file.crash>");
        Console.WriteLine(
            "  crashscript --ast <file.crash>");
        Console.WriteLine(
            "  crashscript --bound <file.crash>");
        Console.WriteLine();

        Console.WriteLine("Commands:");
        Console.WriteLine(
            "  <file.crash>       Execute a CrashScript program.");
        Console.WriteLine(
            "  --check            Validate without executing.");
        Console.WriteLine(
            "  --tokens           Print lexer tokens.");
        Console.WriteLine(
            "  --ast              Print the syntax tree.");
        Console.WriteLine(
            "  --bound            Print the bound and typed tree.");
        Console.WriteLine();

        Console.WriteLine("Options:");
        Console.WriteLine(
            "  -h, --help         Show this help.");
        Console.WriteLine(
            "  -v, --version      Show the version.");
        Console.WriteLine(
            "  --no-color         Disable colored diagnostics.");
        Console.WriteLine();

        Console.WriteLine("Examples:");
        Console.WriteLine(
            "  crashscript examples\\hello.crash");
        Console.WriteLine(
            "  crashscript --check examples\\functions.crash");
        Console.WriteLine(
            "  crashscript --ast examples\\unique-features.crash");
    }
}