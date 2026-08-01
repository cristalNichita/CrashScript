namespace CrashScript.Language.Diagnostics;

public static class DiagnosticCodes
{
    // Lexer errors
    public const string UnexpectedCharacter = "CRASH-L101";
    public const string UnterminatedString = "CRASH-L102";
    public const string InvalidEscapeSequence = "CRASH-L103";
    public const string InvalidNumber = "CRASH-L104";
    
    // Parser errors
    public const string UnexpectedToken = "CRASH-P201";
    public const string ExpectedExpression = "CRASH-P202";
    public const string ExpectedSemicolon = "CRASH-P203";
}