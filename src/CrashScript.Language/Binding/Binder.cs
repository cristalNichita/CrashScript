using CrashScript.Language.Binding.Nodes;
using CrashScript.Language.Binding.Symbols;
using CrashScript.Language.Diagnostics;
using CrashScript.Language.Lexing;
using CrashScript.Language.Syntax;
using CrashScript.Language.Syntax.Expressions;
using CrashScript.Language.Syntax.Statements;

namespace CrashScript.Language.Binding;

public sealed class Binder
{
    private readonly DiagnosticBag _diagnostics = new();

    public BindResult Bind(CompilationUnitSyntax syntax)
    {
        ArgumentNullException.ThrowIfNull(syntax);

        BoundStatement[] statements = syntax.Statements
            .Select(BindStatement)
            .ToArray();

        var root = new BoundCompilationUnit(
            statements,
            syntax.Span);

        return new BindResult(
            root,
            _diagnostics.ToArray());
    }

    private BoundStatement BindStatement(
        StatementSyntax syntax)
    {
        return syntax switch
        {
            ExpressionStatementSyntax expressionStatement =>
                BindExpressionStatement(expressionStatement),

            _ => throw new InvalidOperationException(
                $"Unsupported statement syntax: {syntax.GetType().Name}")
        };
    }

    private BoundExpressionStatement BindExpressionStatement(
        ExpressionStatementSyntax syntax)
    {
        BoundExpression expression =
            BindExpression(syntax.Expression);

        return new BoundExpressionStatement(
            expression,
            syntax.Span);
    }

    private BoundExpression BindExpression(
        ExpressionSyntax syntax)
    {
        return syntax switch
        {
            LiteralExpressionSyntax literal =>
                BindLiteralExpression(literal),

            NameExpressionSyntax name =>
                BindNameExpression(name),

            ParenthesizedExpressionSyntax parenthesized =>
                BindExpression(parenthesized.Expression),

            UnaryExpressionSyntax unary =>
                BindUnaryExpression(unary),

            BinaryExpressionSyntax binary =>
                BindBinaryExpression(binary),

            CallExpressionSyntax call =>
                BindCallExpression(call),

            ErrorExpressionSyntax error =>
                new BoundErrorExpression(error.Span),

            _ => throw new InvalidOperationException(
                $"Unsupported expression syntax: {syntax.GetType().Name}")
        };
    }

    private static BoundExpression BindLiteralExpression(
        LiteralExpressionSyntax syntax)
    {
        TypeSymbol type = syntax.Value switch
        {
            long => TypeSymbol.Int,
            double => TypeSymbol.Float,
            string => TypeSymbol.String,
            bool => TypeSymbol.Bool,
            null => TypeSymbol.Null,

            _ => throw new InvalidOperationException(
                $"Unsupported literal value: {syntax.Value?.GetType().Name}")
        };

        return new BoundLiteralExpression(
            syntax.Value,
            type,
            syntax.Span);
    }

    private BoundExpression BindNameExpression(
        NameExpressionSyntax syntax)
    {
        IReadOnlyList<FunctionSymbol> functions =
            BuiltinFunctions.Find(syntax.Name);

        if (functions.Count > 0)
        {
            _diagnostics.Report(
                DiagnosticCodes.FunctionMustBeCalled,
                DiagnosticCategory.Type,
                $"Function `{syntax.Name}` must be called with parentheses.",
                syntax.Span,
                $"Try `{syntax.Name}(...);`.");

            return new BoundErrorExpression(syntax.Span);
        }

        _diagnostics.Report(
            DiagnosticCodes.UndefinedName,
            DiagnosticCategory.Type,
            $"Name `{syntax.Name}` is not defined.",
            syntax.Span,
            "Variables will become available after they are declared with `memory` or `fixed`.",
            "The computer refuses to invent missing variables.");

        return new BoundErrorExpression(syntax.Span);
    }

    private BoundExpression BindUnaryExpression(
        UnaryExpressionSyntax syntax)
    {
        BoundExpression operand =
            BindExpression(syntax.Operand);

        if (operand.Type == TypeSymbol.Error)
        {
            return new BoundErrorExpression(syntax.Span);
        }

        if (syntax.OperatorToken.Type == TokenType.Minus)
        {
            if (operand.Type == TypeSymbol.Int ||
                operand.Type == TypeSymbol.Float)
            {
                return new BoundUnaryExpression(
                    BoundUnaryOperatorKind.NumericNegation,
                    operand,
                    operand.Type,
                    syntax.Span);
            }
        }

        if (syntax.OperatorToken.Type == TokenType.NotKeyword &&
            operand.Type == TypeSymbol.Bool)
        {
            return new BoundUnaryExpression(
                BoundUnaryOperatorKind.LogicalNegation,
                operand,
                TypeSymbol.Bool,
                syntax.Span);
        }

        _diagnostics.Report(
            DiagnosticCodes.UnaryOperatorNotDefined,
            DiagnosticCategory.Type,
            $"Unary operator `{syntax.OperatorToken.Text}` is not defined for type `{operand.Type.Name}`.",
            syntax.OperatorToken.Span);

        return new BoundErrorExpression(syntax.Span);
    }

    private BoundExpression BindBinaryExpression(
        BinaryExpressionSyntax syntax)
    {
        BoundExpression left =
            BindExpression(syntax.Left);

        BoundExpression right =
            BindExpression(syntax.Right);

        if (left.Type == TypeSymbol.Error ||
            right.Type == TypeSymbol.Error)
        {
            return new BoundErrorExpression(syntax.Span);
        }

        return syntax.OperatorToken.Type switch
        {
            TokenType.Plus =>
                BindAddition(syntax, left, right),

            TokenType.Minus =>
                BindNumericArithmetic(
                    syntax,
                    left,
                    right,
                    BoundBinaryOperatorKind.Subtraction),

            TokenType.Star =>
                BindNumericArithmetic(
                    syntax,
                    left,
                    right,
                    BoundBinaryOperatorKind.Multiplication),

            TokenType.Slash =>
                BindDivision(syntax, left, right),

            TokenType.Percent =>
                BindRemainder(syntax, left, right),

            TokenType.EqualEqual =>
                BindEquality(
                    syntax,
                    left,
                    right,
                    BoundBinaryOperatorKind.Equal),

            TokenType.BangEqual =>
                BindEquality(
                    syntax,
                    left,
                    right,
                    BoundBinaryOperatorKind.NotEqual),

            TokenType.Greater =>
                BindNumericComparison(
                    syntax,
                    left,
                    right,
                    BoundBinaryOperatorKind.Greater),

            TokenType.GreaterEqual =>
                BindNumericComparison(
                    syntax,
                    left,
                    right,
                    BoundBinaryOperatorKind.GreaterOrEqual),

            TokenType.Less =>
                BindNumericComparison(
                    syntax,
                    left,
                    right,
                    BoundBinaryOperatorKind.Less),

            TokenType.LessEqual =>
                BindNumericComparison(
                    syntax,
                    left,
                    right,
                    BoundBinaryOperatorKind.LessOrEqual),

            TokenType.AndKeyword =>
                BindLogicalBinary(
                    syntax,
                    left,
                    right,
                    BoundBinaryOperatorKind.LogicalAnd),

            TokenType.OrKeyword =>
                BindLogicalBinary(
                    syntax,
                    left,
                    right,
                    BoundBinaryOperatorKind.LogicalOr),

            TokenType.QuestionQuestion =>
                BindNullCoalescing(syntax, left, right),

            _ => ReportUndefinedBinaryOperator(
                syntax,
                left,
                right)
        };
    }

    private BoundExpression BindAddition(
        BinaryExpressionSyntax syntax,
        BoundExpression left,
        BoundExpression right)
    {
        if (left.Type == TypeSymbol.String &&
            right.Type == TypeSymbol.String)
        {
            return new BoundBinaryExpression(
                left,
                BoundBinaryOperatorKind.Addition,
                right,
                TypeSymbol.String,
                syntax.Span);
        }

        return BindNumericArithmetic(
            syntax,
            left,
            right,
            BoundBinaryOperatorKind.Addition);
    }

    private BoundExpression BindNumericArithmetic(
        BinaryExpressionSyntax syntax,
        BoundExpression left,
        BoundExpression right,
        BoundBinaryOperatorKind operatorKind)
    {
        if (!IsNumeric(left.Type) ||
            !IsNumeric(right.Type))
        {
            return ReportUndefinedBinaryOperator(
                syntax,
                left,
                right);
        }

        TypeSymbol resultType;

        if (left.Type == TypeSymbol.Int &&
            right.Type == TypeSymbol.Int)
        {
            resultType = TypeSymbol.Int;
        }
        else
        {
            left = Convert(left, TypeSymbol.Float);
            right = Convert(right, TypeSymbol.Float);
            resultType = TypeSymbol.Float;
        }

        return new BoundBinaryExpression(
            left,
            operatorKind,
            right,
            resultType,
            syntax.Span);
    }

    private BoundExpression BindDivision(
        BinaryExpressionSyntax syntax,
        BoundExpression left,
        BoundExpression right)
    {
        if (!IsNumeric(left.Type) ||
            !IsNumeric(right.Type))
        {
            return ReportUndefinedBinaryOperator(
                syntax,
                left,
                right);
        }

        // Division always returns float.
        left = Convert(left, TypeSymbol.Float);
        right = Convert(right, TypeSymbol.Float);

        return new BoundBinaryExpression(
            left,
            BoundBinaryOperatorKind.Division,
            right,
            TypeSymbol.Float,
            syntax.Span);
    }

    private BoundExpression BindRemainder(
        BinaryExpressionSyntax syntax,
        BoundExpression left,
        BoundExpression right)
    {
        if (left.Type != TypeSymbol.Int ||
            right.Type != TypeSymbol.Int)
        {
            return ReportUndefinedBinaryOperator(
                syntax,
                left,
                right);
        }

        return new BoundBinaryExpression(
            left,
            BoundBinaryOperatorKind.Remainder,
            right,
            TypeSymbol.Int,
            syntax.Span);
    }

    private BoundExpression BindNumericComparison(
        BinaryExpressionSyntax syntax,
        BoundExpression left,
        BoundExpression right,
        BoundBinaryOperatorKind operatorKind)
    {
        if (!IsNumeric(left.Type) ||
            !IsNumeric(right.Type))
        {
            return ReportUndefinedBinaryOperator(
                syntax,
                left,
                right);
        }

        PromoteNumbers(
            ref left,
            ref right);

        return new BoundBinaryExpression(
            left,
            operatorKind,
            right,
            TypeSymbol.Bool,
            syntax.Span);
    }

    private BoundExpression BindEquality(
        BinaryExpressionSyntax syntax,
        BoundExpression left,
        BoundExpression right,
        BoundBinaryOperatorKind operatorKind)
    {
        if (IsNumeric(left.Type) &&
            IsNumeric(right.Type))
        {
            PromoteNumbers(
                ref left,
                ref right);

            return new BoundBinaryExpression(
                left,
                operatorKind,
                right,
                TypeSymbol.Bool,
                syntax.Span);
        }

        if (left.Type == right.Type &&
            left.Type != TypeSymbol.Void &&
            left.Type != TypeSymbol.Any)
        {
            return new BoundBinaryExpression(
                left,
                operatorKind,
                right,
                TypeSymbol.Bool,
                syntax.Span);
        }

        if (CanCompareWithNull(left.Type, right.Type))
        {
            return new BoundBinaryExpression(
                left,
                operatorKind,
                right,
                TypeSymbol.Bool,
                syntax.Span);
        }

        return ReportUndefinedBinaryOperator(
            syntax,
            left,
            right);
    }

    private BoundExpression BindLogicalBinary(
        BinaryExpressionSyntax syntax,
        BoundExpression left,
        BoundExpression right,
        BoundBinaryOperatorKind operatorKind)
    {
        if (left.Type != TypeSymbol.Bool ||
            right.Type != TypeSymbol.Bool)
        {
            return ReportUndefinedBinaryOperator(
                syntax,
                left,
                right);
        }

        return new BoundBinaryExpression(
            left,
            operatorKind,
            right,
            TypeSymbol.Bool,
            syntax.Span);
    }

    private BoundExpression BindNullCoalescing(
        BinaryExpressionSyntax syntax,
        BoundExpression left,
        BoundExpression right)
    {
        if (left.Type == TypeSymbol.Null)
        {
            if (right.Type == TypeSymbol.Void)
            {
                return ReportInvalidNullCoalescing(
                    syntax,
                    left,
                    right);
            }

            return new BoundBinaryExpression(
                left,
                BoundBinaryOperatorKind.NullCoalescing,
                right,
                right.Type,
                syntax.Span);
        }

        if (left.Type is NullableTypeSymbol nullableType)
        {
            Conversion underlyingConversion =
                Conversion.Classify(
                    right.Type,
                    nullableType.UnderlyingType);

            if (underlyingConversion.Exists)
            {
                right = Convert(
                    right,
                    nullableType.UnderlyingType);

                return new BoundBinaryExpression(
                    left,
                    BoundBinaryOperatorKind.NullCoalescing,
                    right,
                    nullableType.UnderlyingType,
                    syntax.Span);
            }

            Conversion nullableConversion =
                Conversion.Classify(
                    right.Type,
                    left.Type);

            if (nullableConversion.Exists)
            {
                right = Convert(
                    right,
                    left.Type);

                return new BoundBinaryExpression(
                    left,
                    BoundBinaryOperatorKind.NullCoalescing,
                    right,
                    left.Type,
                    syntax.Span);
            }
        }

        return ReportInvalidNullCoalescing(
            syntax,
            left,
            right);
    }

    private BoundExpression BindCallExpression(
        CallExpressionSyntax syntax)
    {
        BoundExpression[] arguments = syntax.Arguments
            .Select(BindExpression)
            .ToArray();

        if (syntax.Callee is not NameExpressionSyntax nameSyntax)
        {
            _diagnostics.Report(
                DiagnosticCodes.ExpressionIsNotCallable,
                DiagnosticCategory.Type,
                "This expression cannot be called as a function.",
                syntax.Callee.Span);

            return new BoundErrorExpression(syntax.Span);
        }

        IReadOnlyList<FunctionSymbol> candidates =
            BuiltinFunctions.Find(nameSyntax.Name);

        if (candidates.Count == 0)
        {
            _diagnostics.Report(
                DiagnosticCodes.UndefinedName,
                DiagnosticCategory.Type,
                $"Function `{nameSyntax.Name}` is not defined.",
                nameSyntax.Span);

            return new BoundErrorExpression(syntax.Span);
        }

        FunctionSymbol[] sameArity = candidates
            .Where(candidate =>
                candidate.Parameters.Count == arguments.Length)
            .ToArray();

        if (sameArity.Length == 0)
        {
            string expectedCounts = string.Join(
                " or ",
                candidates
                    .Select(candidate =>
                        candidate.Parameters.Count)
                    .Distinct()
                    .Order());

            _diagnostics.Report(
                DiagnosticCodes.IncorrectArgumentCount,
                DiagnosticCategory.Type,
                $"Function `{nameSyntax.Name}` expects {expectedCounts} argument(s), but received {arguments.Length}.",
                syntax.Span);

            return new BoundErrorExpression(syntax.Span);
        }

        var applicableCandidates =
            new List<ApplicableFunction>();

        foreach (FunctionSymbol candidate in sameArity)
        {
            var conversions = new Conversion[arguments.Length];
            int totalCost = 0;
            bool valid = true;

            for (int index = 0;
                 index < arguments.Length;
                 index++)
            {
                Conversion conversion = Conversion.Classify(
                    arguments[index].Type,
                    candidate.Parameters[index].Type);

                conversions[index] = conversion;

                if (!conversion.Exists)
                {
                    valid = false;
                    break;
                }

                totalCost += conversion.Cost;
            }

            if (valid)
            {
                applicableCandidates.Add(
                    new ApplicableFunction(
                        candidate,
                        conversions,
                        totalCost));
            }
        }

        ApplicableFunction? selected = applicableCandidates
            .OrderBy(candidate => candidate.TotalCost)
            .FirstOrDefault();

        if (selected is null)
        {
            string argumentTypes = string.Join(
                ", ",
                arguments.Select(argument =>
                    argument.Type.Name));

            string availableSignatures = string.Join(
                Environment.NewLine,
                sameArity.Select(candidate =>
                    $"  {candidate.Signature}"));

            _diagnostics.Report(
                DiagnosticCodes.NoMatchingOverload,
                DiagnosticCategory.Type,
                $"No overload of `{nameSyntax.Name}` accepts argument types ({argumentTypes}).",
                syntax.Span,
                $"Available overloads:{Environment.NewLine}{availableSignatures}");

            return new BoundErrorExpression(syntax.Span);
        }

        BoundExpression[] convertedArguments =
            new BoundExpression[arguments.Length];

        for (int index = 0;
             index < arguments.Length;
             index++)
        {
            TypeSymbol parameterType =
                selected.Function.Parameters[index].Type;

            convertedArguments[index] = Convert(
                arguments[index],
                parameterType);
        }

        return new BoundCallExpression(
            selected.Function,
            convertedArguments,
            selected.Function.ReturnType,
            syntax.Span);
    }

    private BoundExpression Convert(
        BoundExpression expression,
        TypeSymbol targetType)
    {
        Conversion conversion = Conversion.Classify(
            expression.Type,
            targetType);

        if (!conversion.Exists)
        {
            throw new InvalidOperationException(
                $"Cannot convert `{expression.Type.Name}` to `{targetType.Name}`.");
        }

        if (conversion.IsIdentity)
        {
            return expression;
        }

        return new BoundConversionExpression(
            expression,
            targetType,
            expression.Span);
    }

    private static void PromoteNumbers(
        ref BoundExpression left,
        ref BoundExpression right)
    {
        if (left.Type == TypeSymbol.Float &&
            right.Type == TypeSymbol.Int)
        {
            right = new BoundConversionExpression(
                right,
                TypeSymbol.Float,
                right.Span);
        }
        else if (left.Type == TypeSymbol.Int &&
                 right.Type == TypeSymbol.Float)
        {
            left = new BoundConversionExpression(
                left,
                TypeSymbol.Float,
                left.Span);
        }
    }

    private BoundExpression ReportUndefinedBinaryOperator(
        BinaryExpressionSyntax syntax,
        BoundExpression left,
        BoundExpression right)
    {
        _diagnostics.Report(
            DiagnosticCodes.BinaryOperatorNotDefined,
            DiagnosticCategory.Type,
            $"Binary operator `{syntax.OperatorToken.Text}` is not defined for types `{left.Type.Name}` and `{right.Type.Name}`.",
            syntax.OperatorToken.Span);

        return new BoundErrorExpression(syntax.Span);
    }

    private BoundExpression ReportInvalidNullCoalescing(
        BinaryExpressionSyntax syntax,
        BoundExpression left,
        BoundExpression right)
    {
        _diagnostics.Report(
            DiagnosticCodes.InvalidNullCoalescing,
            DiagnosticCategory.Type,
            $"Operator `??` cannot be applied to `{left.Type.Name}` and `{right.Type.Name}`.",
            syntax.OperatorToken.Span,
            "The left operand of `??` must be nullable.");

        return new BoundErrorExpression(syntax.Span);
    }

    private static bool IsNumeric(TypeSymbol type)
    {
        return type == TypeSymbol.Int ||
               type == TypeSymbol.Float;
    }

    private static bool CanCompareWithNull(
        TypeSymbol left,
        TypeSymbol right)
    {
        return left == TypeSymbol.Null &&
               (right == TypeSymbol.Null || right.IsNullable)
               ||
               right == TypeSymbol.Null &&
               left.IsNullable;
    }

    private sealed record ApplicableFunction(
        FunctionSymbol Function,
        IReadOnlyList<Conversion> Conversions,
        int TotalCost);
}