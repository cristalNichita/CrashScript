namespace CrashScript.Cli;

public static class ExitCodes
{
    public const int Success = 0;
    
    public const int InvalidArguments = 1;
    public const int FileError = 2;

    public const int LexerError = 3;
    public const int ParserError = 4;
    public const int TypeError = 5;
    public const int RuntimeError = 6;
    
    public const int InternalError = 7;
}