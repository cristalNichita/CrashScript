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

    // Type and binding errors
    public const string UndefinedName = "CRASH-T301";
    public const string UnaryOperatorNotDefined = "CRASH-T302";
    public const string BinaryOperatorNotDefined = "CRASH-T303";
    public const string FunctionMustBeCalled = "CRASH-T304";
    public const string ExpressionIsNotCallable = "CRASH-T305";
    public const string IncorrectArgumentCount = "CRASH-T306";
    public const string NoMatchingOverload = "CRASH-T307";
    public const string InvalidNullCoalescing = "CRASH-T308";

    // Runtime errors
    public const string GuardFailed = "CRASH-R401";
    public const string DivisionByZero = "CRASH-R402";
    public const string NativeFunctionFailed = "CRASH-R403";
    public const string NumericOverflow = "CRASH-R404";
    public const string InvalidRandomRange = "CRASH-R405";
}