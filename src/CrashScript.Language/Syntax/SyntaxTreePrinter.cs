using System.Globalization;
using System.Text;
using CrashScript.Language.Syntax.Expressions;
using CrashScript.Language.Syntax.Statements;

namespace CrashScript.Language.Syntax;

public static class SyntaxTreePrinter
{
    public static string Print(SyntaxNode root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var builder = new StringBuilder();

        WriteNode(
            builder,
            root,
            string.Empty,
            true);

        return builder.ToString().TrimEnd();
    }

    private static void WriteNode(
        StringBuilder builder,
        SyntaxNode node,
        string indent,
        bool isLast)
    {
        builder.Append(indent);
        builder.Append(isLast ? "└── " : "├── ");
        builder.AppendLine(GetLabel(node));

        string childIndent =
            indent + (isLast ? "    " : "│   ");

        SyntaxNode[] children = GetChildren(node).ToArray();

        for (int index = 0; index < children.Length; index++)
        {
            WriteNode(
                builder,
                children[index],
                childIndent,
                index == children.Length - 1);
        }
    }

    private static IEnumerable<SyntaxNode> GetChildren(
        SyntaxNode node)
    {
        return node switch
        {
            CompilationUnitSyntax compilationUnit =>
                compilationUnit.Statements,

            ExpressionStatementSyntax expressionStatement =>
                [expressionStatement.Expression],

            ParenthesizedExpressionSyntax parenthesizedExpression =>
                [parenthesizedExpression.Expression],

            UnaryExpressionSyntax unaryExpression =>
                [unaryExpression.Operand],

            BinaryExpressionSyntax binaryExpression =>
                [
                    binaryExpression.Left,
                    binaryExpression.Right
                ],

            CallExpressionSyntax callExpression =>
                new SyntaxNode[]
                {
                    callExpression.Callee
                }.Concat(callExpression.Arguments),

            _ => []
        };
    }

    private static string GetLabel(SyntaxNode node)
    {
        return node switch
        {
            CompilationUnitSyntax =>
                "CompilationUnit",

            ExpressionStatementSyntax =>
                "ExpressionStatement",

            LiteralExpressionSyntax literal =>
                $"LiteralExpression ({FormatValue(literal.Value)})",

            NameExpressionSyntax name =>
                $"NameExpression ({name.Name})",

            ParenthesizedExpressionSyntax =>
                "ParenthesizedExpression",

            UnaryExpressionSyntax unary =>
                $"UnaryExpression ({unary.OperatorToken.Text})",

            BinaryExpressionSyntax binary =>
                $"BinaryExpression ({binary.OperatorToken.Text})",

            CallExpressionSyntax =>
                "CallExpression",

            ErrorExpressionSyntax =>
                "ErrorExpression",

            _ => node.GetType().Name
        };
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "null",
            string text => $"\"{Escape(text)}\"",
            bool boolean => boolean ? "true" : "false",
            IFormattable formattable =>
                formattable.ToString(
                    null,
                    CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "null"
        };
    }

    private static string Escape(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t")
            .Replace("\"", "\\\"");
    }
}