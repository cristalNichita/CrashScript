using System.Globalization;
using System.Text;
using System.Xml.Serialization;
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

            BoundBlockStatement block =>
                block.Statements,

            BoundIfStatement ifStatement =>
                GetIfChildren(ifStatement),

            BoundIfBranch branch =>
            [
                branch.Condition,
                branch.Body
            ],

            BoundWhileStatement whileStatement =>
            [
                whileStatement.Condition,
                whileStatement.Body
            ],

            BoundVariableDeclaration declaration =>
                [declaration.Initializer],

            BoundExpressionStatement expressionStatement =>
                [expressionStatement.Expression],

            BoundAssignmentExpression assignment =>
                [assignment.Expression],

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
            
            BoundFunctionDeclaration function =>
                [function.Body],

            BoundReturnStatement returnStatement =>
                returnStatement.Expression is null
                    ? []
                    : [returnStatement.Expression],

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
            
            BoundVariableDeclaration declaration =>
                $"BoundVariableDeclaration ({declaration.Variable})",

            BoundVariableExpression variable =>
                $"BoundVariableExpression ({variable.Variable.Name} : {variable.Type.Name})",

            BoundAssignmentExpression assignment =>
                $"BoundAssignmentExpression ({assignment.Variable.Name} : {assignment.Type.Name})",
            
            BoundBlockStatement =>
                "BoundBlockStatement",

            BoundIfStatement =>
                "BoundIfStatement",

            BoundIfBranch =>
                "BoundIfBranch",

            BoundWhileStatement =>
                "BoundWhileStatement",
            
            BoundFunctionDeclaration function =>
                $"BoundProcessDeclaration ({function.Function.Signature})",

            BoundReturnStatement =>
                "BoundReturnStatement",

            _ => node.GetType().Name
        };
    }
    
    private static IEnumerable<BoundNode> GetIfChildren(
        BoundIfStatement statement)
    {
        foreach (BoundIfBranch branch in statement.Branches)
        {
            yield return branch;
        }

        if (statement.ElseBody is not null)
        {
            yield return statement.ElseBody;
        }
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