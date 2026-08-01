namespace CrashScript.Language.Binding;

public enum BoundBinaryOperatorKind
{
    Addition,
    Subtraction,
    Multiplication,
    Division,
    Remainder,
    
    Equal,
    NotEqual,
    Greater,
    GreaterOrEqual,
    Less,
    LessOrEqual,
    
    LogicalAnd,
    LogicalOr,
    
    NullCoalescing
}