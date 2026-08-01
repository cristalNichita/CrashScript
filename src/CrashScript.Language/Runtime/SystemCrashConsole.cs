namespace CrashScript.Language.Runtime;

public sealed class SystemCrashConsole : ICrashConsole
{
    public void Write(string text)
    {
        Console.Write(text);
    }

    public void WriteLine(string text)
    {
        Console.WriteLine(text);
    }

    public string? ReadLine()
    {
        return Console.ReadLine();
    }
}