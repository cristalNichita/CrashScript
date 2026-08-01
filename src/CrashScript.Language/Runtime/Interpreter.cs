using CrashScript.Language.Binding;
using CrashScript.Language.Binding.Nodes;
using CrashScript.Language.Binding.Symbols;
using CrashScript.Language.Diagnostics;
using CrashScript.Language.Source;

namespace CrashScript.Language.Runtime;

public sealed class Interpreter
{
    private readonly NativeFunctionDispatcher _nativeFunctions;
    private readonly RuntimeEnvironment _environment;

    public Interpreter(
        ICrashConsole? console = null,
        Random? random = null,
        RuntimeEnvironment? environment = null)
    {
        console ??= new SystemCrashConsole();
        random ??= Random.Shared;

        _environment =
            environment ?? new RuntimeEnvironment();

        _nativeFunctions =
            new NativeFunctionDispatcher(
                console,
                random);
    }

    public ExecutionResult Execute(
        BoundCompilationUnit program)
    {
        ArgumentNullException.ThrowIfNull(program);

        object? lastValue = null;

        try
        {
            foreach (BoundStatement statement in program.Statements)
            {
                lastValue = ExecuteStatement(statement);
            }

            return new ExecutionResult(
                lastValue,
                [],
                Executed: true);
        }
        catch (RuntimeFault fault)
        {
            return new ExecutionResult(
                lastValue,
                [fault.ToDiagnostic()],
                Executed: true);
        }
    }

    private object? ExecuteStatement(
        BoundStatement statement)
    {
        return statement switch
        {
            BoundVariableDeclaration declaration =>
                ExecuteVariableDeclaration(declaration),

            BoundExpressionStatement expressionStatement =>
                EvaluateExpression(
                    expressionStatement.Expression),

            _ => throw new InvalidOperationException(
                $"Unsupported bound statement: {statement.GetType().Name}")
        };
    }
    
    private object? ExecuteVariableDeclaration(
        BoundVariableDeclaration declaration)
    {
        object? value =
            EvaluateExpression(
                declaration.Initializer);

        _environment.Define(
            declaration.Variable,
            value);

        return null;
    }

    private object? EvaluateExpression(
        BoundExpression expression)
    {
        return expression switch
        {
            BoundLiteralExpression literal =>
                literal.Value,

            BoundVariableExpression variable =>
                _environment.Get(
                    variable.Variable),

            BoundAssignmentExpression assignment =>
                EvaluateAssignmentExpression(
                    assignment),

            BoundConversionExpression conversion =>
                EvaluateConversion(conversion),

            BoundUnaryExpression unary =>
                EvaluateUnaryExpression(unary),

            BoundBinaryExpression binary =>
                EvaluateBinaryExpression(binary),

            BoundCallExpression call =>
                EvaluateCallExpression(call),

            BoundErrorExpression =>
                throw new InvalidOperationException(
                    "An error expression reached the interpreter."),

            _ => throw new InvalidOperationException(
                $"Unsupported bound expression: {expression.GetType().Name}")
        };
    }
    
    private object? EvaluateAssignmentExpression(
        BoundAssignmentExpression assignment)
    {
        object? value =
            EvaluateExpression(
                assignment.Expression);

        _environment.Assign(
            assignment.Variable,
            value);

        return value;
    }

    private object? EvaluateConversion(
        BoundConversionExpression expression)
    {
        object? value = EvaluateExpression(
            expression.Expression);

        TypeSymbol sourceType =
            expression.Expression.Type;

        TypeSymbol targetType =
            expression.Type;

        if (sourceType == targetType ||
            targetType == TypeSymbol.Any)
        {
            return value;
        }

        if (sourceType == TypeSymbol.Int &&
            targetType == TypeSymbol.Float)
        {
            return (double)(long)value!;
        }

        if (targetType is NullableTypeSymbol)
        {
            return value;
        }

        throw new InvalidOperationException(
            $"Unsupported runtime conversion from `{sourceType.Name}` to `{targetType.Name}`.");
    }

    private object? EvaluateUnaryExpression(
        BoundUnaryExpression expression)
    {
        object? operand = EvaluateExpression(
            expression.Operand);

        return expression.OperatorKind switch
        {
            BoundUnaryOperatorKind.NumericNegation =>
                NegateNumber(
                    operand,
                    expression.Span),

            BoundUnaryOperatorKind.LogicalNegation =>
                !(bool)operand!,

            _ => throw new InvalidOperationException(
                $"Unsupported unary operator: {expression.OperatorKind}")
        };
    }

    private object? EvaluateBinaryExpression(
        BoundBinaryExpression expression)
    {
        // These operators must not always evaluate the right side.
        if (expression.OperatorKind ==
            BoundBinaryOperatorKind.LogicalAnd)
        {
            bool left = (bool)EvaluateExpression(
                expression.Left)!;

            return left &&
                   (bool)EvaluateExpression(
                       expression.Right)!;
        }

        if (expression.OperatorKind ==
            BoundBinaryOperatorKind.LogicalOr)
        {
            bool left = (bool)EvaluateExpression(
                expression.Left)!;

            return left ||
                   (bool)EvaluateExpression(
                       expression.Right)!;
        }

        if (expression.OperatorKind ==
            BoundBinaryOperatorKind.NullCoalescing)
        {
            object? left = EvaluateExpression(
                expression.Left);

            return left ??
                   EvaluateExpression(
                       expression.Right);
        }

        object? leftValue = EvaluateExpression(
            expression.Left);

        object? rightValue = EvaluateExpression(
            expression.Right);

        return expression.OperatorKind switch
        {
            BoundBinaryOperatorKind.Addition =>
                Add(
                    leftValue,
                    rightValue,
                    expression.Span),

            BoundBinaryOperatorKind.Subtraction =>
                Subtract(
                    leftValue,
                    rightValue,
                    expression.Span),

            BoundBinaryOperatorKind.Multiplication =>
                Multiply(
                    leftValue,
                    rightValue,
                    expression.Span),

            BoundBinaryOperatorKind.Division =>
                Divide(
                    leftValue,
                    rightValue,
                    expression.Span),

            BoundBinaryOperatorKind.Remainder =>
                Remainder(
                    leftValue,
                    rightValue,
                    expression.Span),

            BoundBinaryOperatorKind.Equal =>
                Equals(
                    leftValue,
                    rightValue),

            BoundBinaryOperatorKind.NotEqual =>
                !Equals(
                    leftValue,
                    rightValue),

            BoundBinaryOperatorKind.Greater =>
                CompareNumbers(
                    leftValue,
                    rightValue) > 0,

            BoundBinaryOperatorKind.GreaterOrEqual =>
                CompareNumbers(
                    leftValue,
                    rightValue) >= 0,

            BoundBinaryOperatorKind.Less =>
                CompareNumbers(
                    leftValue,
                    rightValue) < 0,

            BoundBinaryOperatorKind.LessOrEqual =>
                CompareNumbers(
                    leftValue,
                    rightValue) <= 0,

            _ => throw new InvalidOperationException(
                $"Unsupported binary operator: {expression.OperatorKind}")
        };
    }

    private object? EvaluateCallExpression(
        BoundCallExpression expression)
    {
        object?[] arguments = expression.Arguments
            .Select(EvaluateExpression)
            .ToArray();

        return _nativeFunctions.Invoke(
            expression.Function,
            arguments,
            expression.Span);
    }

    private static object NegateNumber(
        object? value,
        SourceSpan span)
    {
        return value switch
        {
            long integer =>
                ExecuteCheckedInteger(
                    () => checked(-integer),
                    span),

            double floatingPoint =>
                -floatingPoint,

            _ => throw new InvalidOperationException(
                "Numeric negation received a non-numeric value.")
        };
    }

    private static object Add(
        object? left,
        object? right,
        SourceSpan span)
    {
        if (left is long leftInteger &&
            right is long rightInteger)
        {
            return ExecuteCheckedInteger(
                () => checked(
                    leftInteger + rightInteger),
                span);
        }

        if (left is double leftFloat &&
            right is double rightFloat)
        {
            return leftFloat + rightFloat;
        }

        if (left is string leftString &&
            right is string rightString)
        {
            return leftString + rightString;
        }

        throw new InvalidOperationException(
            $"Addition received incompatible runtime values: " +
            $"`{GetRuntimeTypeName(left)}` and `{GetRuntimeTypeName(right)}`.");
    }

    private static object Subtract(
        object? left,
        object? right,
        SourceSpan span)
    {
        return (left, right) switch
        {
            (long leftInteger, long rightInteger) =>
                ExecuteCheckedInteger(
                    () => checked(
                        leftInteger - rightInteger),
                    span),

            (double leftFloat, double rightFloat) =>
                leftFloat - rightFloat,

            _ => throw new InvalidOperationException(
                "Subtraction received incompatible runtime values.")
        };
    }

    private static object Multiply(
        object? left,
        object? right,
        SourceSpan span)
    {
        if (left is long leftInteger &&
            right is long rightInteger)
        {
            return ExecuteCheckedInteger(
                () => checked(
                    leftInteger * rightInteger),
                span);
        }

        if (left is double leftFloat &&
            right is double rightFloat)
        {
            return leftFloat * rightFloat;
        }

        throw new InvalidOperationException(
            $"Multiplication received incompatible runtime values: " +
            $"`{GetRuntimeTypeName(left)}` and `{GetRuntimeTypeName(right)}`.");
    }

    private static object Divide(
        object? left,
        object? right,
        SourceSpan span)
    {
        double leftNumber = (double)left!;
        double rightNumber = (double)right!;

        if (rightNumber == 0)
        {
            throw new RuntimeFault(
                DiagnosticCodes.DivisionByZero,
                "Division by zero.",
                span,
                "The right operand of `/` must not be zero.",
                "Even CrashScript cannot divide reality into zero pieces.");
        }

        return leftNumber / rightNumber;
    }

    private static object Remainder(
        object? left,
        object? right,
        SourceSpan span)
    {
        long leftNumber = (long)left!;
        long rightNumber = (long)right!;

        if (rightNumber == 0)
        {
            throw new RuntimeFault(
                DiagnosticCodes.DivisionByZero,
                "Remainder by zero.",
                span,
                "The right operand of `%` must not be zero.");
        }

        return ExecuteCheckedInteger(
            () => checked(
                leftNumber % rightNumber),
            span);
    }

    private static int CompareNumbers(
        object? left,
        object? right)
    {
        return (left, right) switch
        {
            (long leftInteger, long rightInteger) =>
                leftInteger.CompareTo(
                    rightInteger),

            (double leftFloat, double rightFloat) =>
                leftFloat.CompareTo(
                    rightFloat),

            _ => throw new InvalidOperationException(
                "Numeric comparison received incompatible runtime values.")
        };
    }

    private static long ExecuteCheckedInteger(
        Func<long> operation,
        SourceSpan span)
    {
        try
        {
            return operation();
        }
        catch (OverflowException)
        {
            throw new RuntimeFault(
                DiagnosticCodes.NumericOverflow,
                "Integer arithmetic overflow.",
                span,
                "CrashScript integers use signed 64-bit values.",
                "The number escaped the available memory before the program could.");
        }
    }
    
    private static string GetRuntimeTypeName(object? value)
    {
        return value?.GetType().FullName ?? "null";
    }
}