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

        SyntaxNode[] children =
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

    private static IEnumerable<SyntaxNode> GetChildren(
        SyntaxNode node)
    {
        return node switch
        {
            CompilationUnitSyntax compilationUnit =>
                compilationUnit.Statements,

            VariableDeclarationStatementSyntax declaration =>
                [declaration.Initializer],

            BlockStatementSyntax block =>
                block.Statements,

            IfStatementSyntax ifStatement =>
                GetIfChildren(ifStatement),

            IfBranchSyntax branch =>
            [
                branch.Condition,
                branch.Body
            ],

            ElseClauseSyntax elseClause =>
                [elseClause.Body],

            WhileStatementSyntax whileStatement =>
            [
                whileStatement.Condition,
                whileStatement.Body
            ],

            ExpressionStatementSyntax expressionStatement =>
                [expressionStatement.Expression],

            AssignmentExpressionSyntax assignment =>
            [
                assignment.Target,
                assignment.Value
            ],

            ParenthesizedExpressionSyntax parenthesized =>
                [parenthesized.Expression],

            UnaryExpressionSyntax unary =>
                [unary.Operand],

            BinaryExpressionSyntax binary =>
            [
                binary.Left,
                binary.Right
            ],

            CallExpressionSyntax call =>
                new SyntaxNode[]
                {
                    call.Callee
                }.Concat(call.Arguments),

            _ => []
        };
    }

    private static string GetLabel(SyntaxNode node)
    {
        return node switch
        {
            CompilationUnitSyntax =>
                "CompilationUnit",

            VariableDeclarationStatementSyntax declaration =>
                GetVariableDeclarationLabel(declaration),

            ExpressionStatementSyntax =>
                "ExpressionStatement",

            AssignmentExpressionSyntax =>
                "AssignmentExpression",

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

            TypeSyntax type =>
                $"TypeSyntax ({type.Name}{(type.IsNullable ? "?" : string.Empty)})",
            
            BlockStatementSyntax =>
                "BlockStatement",

            IfStatementSyntax =>
                "IfStatement",

            IfBranchSyntax branch =>
                branch.ElseKeyword is null
                    ? "IfBranch"
                    : "ElseIfBranch",

            ElseClauseSyntax =>
                "ElseClause",

            WhileStatementSyntax =>
                "WhileStatement",

            _ => node.GetType().Name
        };
    }

    private static string GetVariableDeclarationLabel(
        VariableDeclarationStatementSyntax declaration)
    {
        string keyword =
            declaration.IsFixed
                ? "fixed"
                : "memory";

        string type = declaration.UsesTypeInference
            ? "inferred"
            : declaration.DeclaredType is null
                ? "<missing>"
                : declaration.DeclaredType.Name +
                  (declaration.DeclaredType.IsNullable
                      ? "?"
                      : string.Empty);

        return
            $"VariableDeclaration ({keyword} {declaration.Name} : {type})";
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
    
    private static IEnumerable<SyntaxNode> GetIfChildren(
        IfStatementSyntax statement)
    {
        foreach (IfBranchSyntax branch in statement.Branches)
        {
            yield return branch;
        }

        if (statement.ElseClause is not null)
        {
            yield return statement.ElseClause;
        }
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