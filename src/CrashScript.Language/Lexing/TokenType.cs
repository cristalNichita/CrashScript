namespace CrashScript.Language.Lexing;

public enum TokenType
{
    BadToken,
    EndOfFile,
    
    // Literals and identifiers
    Identifier,
    IntegerLiteral,
    FloatLiteral,
    StringLiteral,
    
    // Punctuation
    LeftParenthesis,
    RightParenthesis,
    Comma,
    Colon,
    Semicolon,
    Question,
    
    // Arithmetic operators
    Plus,
    Minus,
    Star,
    Slash,
    Percent,
    
    // Assignment and compound syntax
    Equal,
    ColonEqual,
    Arrow,
    FatArrow,
    QuestionQuestion,
    
    // Comparison operators
    EqualEqual,
    BangEqual,
    Greater,
    GreaterEqual,
    Less,
    LessEqual,
    
    // Declarations
    MemoryKeyword,
    FixedKeyword,
    ProcessKeyword,
    ReturnKeyword,
    
    // Control flow
    IfKeyword,
    ThenKeyword,
    ElseKeyword,
    WhileKeyword,
    DoKeyword,
    EndKeyword,
    
    // Logical operators
    AndKeyword,
    OrKeyword,
    NotKeyword,
    
    // CrashScript-specific syntax
    SelectKeyword,
    WhenKeyword,
    GuardKeyword,
    
    // Literal keywords
    TrueKeyword,
    FalseKeyword,
    NullKeyword,
    
    // Built-in types
    IntKeyword,
    FloatKeyword,
    StringKeyword,
    BoolKeyword,
    VoidKeyword
}