namespace CrashScript.Language.Runtime;

public interface ICrashConsole
{
    void Write(string text);
    void WriteLine(string text);
    string? ReadLine();
}