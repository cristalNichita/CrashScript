namespace CrashScript.Language.Runtime;

/// <summary>
/// Internal control-flow signal used to leave nested blocks
/// when a CrashScript process executes return.
/// </summary>
public sealed class ReturnSignal : Exception
{
    public ReturnSignal(object? value)
    {
        Value = value;
    }
    
    public object? Value { get; }
}