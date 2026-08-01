using System.Globalization;
using System.Text;
using CrashScript.Language.Binding.Nodes;

namespace CrashScript.Language.Binding;

public static class BoundTreePrinter
{
    public static string Print(BoundNode root)
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
        BoundNode node,
        string indent,
        bool isLast)
    {
        builder.Append(indent);
        builder.Append(isLast ? "└── " : "├── ");
        builder.AppendLine(GetLabel(node));

        string childIndent =
            indent + (isLast ? "    " : "│   ");

        BoundNode[] children =
            GetChildren(node).ToArray();

        for (int index = 0;
             index < children.Length;
             index++)
        {
            WriteNode(
                builder,
                children[index],
                childIndent,
                index == children.Length - 1);
        }
    }

    private static IEnumerable<BoundNode> GetChildren(
        BoundNode node)
    {
        return node switch
        {
            BoundCompilationUnit compilationUnit =>
                compilationUnit.Statements,

            BoundExpressionStatement expressionStatement =>
                [expressionStatement.Expression],

            BoundConversionExpression conversion =>
                [conversion.Expression],

            BoundUnaryExpression unary =>
                [unary.Operand],

            BoundBinaryExpression binary =>
                [
                    binary.Left,
                    binary.Right
                ],

            BoundCallExpression call =>
                call.Arguments,

            _ => []
        };
    }

    private static string GetLabel(BoundNode node)
    {
        return node switch
        {
            BoundCompilationUnit =>
                "BoundCompilationUnit",

            BoundExpressionStatement =>
                "BoundExpressionStatement",

            BoundLiteralExpression literal =>
                $"BoundLiteralExpression ({FormatValue(literal.Value)} : {literal.Type.Name})",

            BoundConversionExpression conversion =>
                $"BoundConversionExpression (to {conversion.Type.Name})",

            BoundUnaryExpression unary =>
                $"BoundUnaryExpression ({unary.OperatorKind} : {unary.Type.Name})",

            BoundBinaryExpression binary =>
                $"BoundBinaryExpression ({binary.OperatorKind} : {binary.Type.Name})",

            BoundCallExpression call =>
                $"BoundCallExpression ({call.Function.Signature})",

            BoundErrorExpression =>
                "BoundErrorExpression",

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