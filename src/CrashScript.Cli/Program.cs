using CrashScript.Language;
using CrashScript.Language.Source;

namespace CrashScript.Cli;

internal static class Program
{
    private const string Version = "0.1.0";
    
    private const int SuccessExitCode = 0;
    private const int InvalidArgumentExitCode = 1;
    private const int FileErrorExitCode = 2;

    public static int Main(string[] args)
    {
        if (args.Length == 1 && IsVersionArgument(args[0]))
        {
            Console.WriteLine($"CrashScript {Version}");
            return SuccessExitCode;
        }

        if (args.Length != 1)
        {
            PrintUsage();
            return InvalidArgumentExitCode;
        }

        try
        {
            var engine = new CrashScriptEngine();
            SourceText source = engine.LoadSourceFile(args[0]);

            PrintLoadedSource(source);

            return SuccessExitCode;
        }
        catch (FileNotFoundException exception)
        {
            PrintError(exception.Message);
            return FileErrorExitCode;
        }
        catch (InvalidDataException exception)
        {
            PrintError(exception.Message);
            return FileErrorExitCode;
        }
        catch (UnauthorizedAccessException exception)
        {
            PrintError(exception.Message);
            return FileErrorExitCode;
        }
        catch (IOException exception)
        {
            PrintError(exception.Message);
            return FileErrorExitCode;
        }
    }

    private static bool IsVersionArgument(string argument)
    {
        return argument is "--version" or "-v";
    }

    private static void PrintLoadedSource(SourceText source)
    {
        Console.WriteLine($"CrashScript {Version}");
        Console.WriteLine($"Loaded: {source.FileName}");
        Console.WriteLine($"Path: {source.FilePath}");
        Console.WriteLine($"Characters: {source.Length}");
        Console.WriteLine();
        
        Console.WriteLine("Source:");
        Console.WriteLine("---------------------------------");
        Console.WriteLine(source.Text);

        if (!source.Text.EndsWith('\n'))
        {
            Console.WriteLine();
        }
        
        Console.WriteLine("---------------------------------");
        Console.WriteLine();
        Console.WriteLine("Lexer, parser and interpreter are not implemented yet.");
    }

    private static void PrintUsage()
    {
        Console.WriteLine($"CrashScript {Version}");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine(" crashscript <file.crash>");
        Console.WriteLine(" crashscript --version");
        Console.WriteLine();
        Console.WriteLine("Development");
        Console.WriteLine(" dotnet run --project src/CrashScript.Cli -- examples/hello.crash");
    }

    private static void PrintError(string message)
    {
        Console.Error.WriteLine("CrashScript could not start");
        Console.Error.WriteLine(message);
    }
}