using CrashScript.Language.Binding;
using CrashScript.Language.Binding.Nodes;
using CrashScript.Language.Binding.Symbols;
using CrashScript.Language.Diagnostics;
using CrashScript.Language.Source;

namespace CrashScript.Language.Runtime;

public sealed class Interpreter
{
    private readonly NativeFunctionDispatcher _nativeFunctions;
    private readonly RuntimeEnvironment _globalEnvironment;

    private readonly Dictionary<
        FunctionSymbol,
        BoundFunctionDeclaration> _userFunctions = [];
    
    private RuntimeEnvironment _environment;

    public Interpreter(
        ICrashConsole? console = null,
        Random? random = null,
        RuntimeEnvironment? environment = null)
    {
        console ??= new SystemCrashConsole();
        random ??= Random.Shared;

        _globalEnvironment =
            environment ?? new RuntimeEnvironment();

        _environment =
            _globalEnvironment;

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

        _userFunctions.Clear();

        foreach (BoundFunctionDeclaration declaration
                 in program.Statements
                     .OfType<BoundFunctionDeclaration>())
        {
            _userFunctions.Add(
                declaration.Function,
                declaration);
        }

        try
        {
            foreach (BoundStatement statement in program.Statements)
            {
                if (statement is BoundFunctionDeclaration)
                {
                    continue;
                }

                lastValue =
                    ExecuteStatement(statement);
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
            BoundFunctionDeclaration =>
                null,

            BoundReturnStatement returnStatement =>
                ExecuteReturnStatement(returnStatement),

            BoundVariableDeclaration declaration =>
                ExecuteVariableDeclaration(declaration),

            BoundBlockStatement block =>
                ExecuteBlockStatement(block),

            BoundIfStatement ifStatement =>
                ExecuteIfStatement(ifStatement),

            BoundWhileStatement whileStatement =>
                ExecuteWhileStatement(whileStatement),

            BoundExpressionStatement expressionStatement =>
                EvaluateExpression(
                    expressionStatement.Expression),

            _ => throw new InvalidOperationException(
                $"Unsupported bound statement: {statement.GetType().Name}")
        };
    }
    
    private object? ExecuteReturnStatement(
        BoundReturnStatement statement)
    {
        object? value =
            statement.Expression is null
                ? null
                : EvaluateExpression(
                    statement.Expression);

        throw new ReturnSignal(value);
    }
    
    private object? ExecuteBlockStatement(
        BoundBlockStatement block)
    {
        RuntimeEnvironment previousEnvironment =
            _environment;

        _environment =
            new RuntimeEnvironment(
                previousEnvironment);

        try
        {
            object? lastValue = null;

            foreach (BoundStatement statement in block.Statements)
            {
                lastValue =
                    ExecuteStatement(statement);
            }

            return lastValue;
        }
        finally
        {
            _environment =
                previousEnvironment;
        }
    }
    
    private object? ExecuteIfStatement(
        BoundIfStatement statement)
    {
        foreach (BoundIfBranch branch in statement.Branches)
        {
            bool condition =
                (bool)EvaluateExpression(
                    branch.Condition)!;

            if (condition)
            {
                return ExecuteBlockStatement(
                    branch.Body);
            }
        }

        if (statement.ElseBody is not null)
        {
            return ExecuteBlockStatement(
                statement.ElseBody);
        }

        return null;
    }
    
    private object? ExecuteWhileStatement(
        BoundWhileStatement statement)
    {
        object? lastValue = null;

        while ((bool)EvaluateExpression(
                   statement.Condition)!)
        {
            lastValue =
                ExecuteBlockStatement(
                    statement.Body);
        }

        return lastValue;
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

        if (expression.Function.IsNative)
        {
            return _nativeFunctions.Invoke(
                expression.Function,
                arguments,
                expression.Span);
        }

        return InvokeUserFunction(
            expression.Function,
            arguments);
    }
    
    private object? InvokeUserFunction(
        FunctionSymbol function,
        IReadOnlyList<object?> arguments)
    {
        if (!_userFunctions.TryGetValue(
                function,
                out BoundFunctionDeclaration? declaration))
        {
            throw new InvalidOperationException(
                $"Runtime declaration for process `{function.Name}` was not found.");
        }

        RuntimeEnvironment previousEnvironment =
            _environment;

        _environment =
            new RuntimeEnvironment(
                _globalEnvironment);

        try
        {
            for (int index = 0;
                 index < function.Parameters.Count;
                 index++)
            {
                _environment.Define(
                    function.Parameters[index],
                    arguments[index]);
            }

            try
            {
                object? lastValue = null;

                foreach (BoundStatement statement
                         in declaration.Body.Statements)
                {
                    lastValue =
                        ExecuteStatement(statement);
                }

                return lastValue;
            }
            catch (ReturnSignal signal)
            {
                return signal.Value;
            }
        }
        finally
        {
            _environment =
                previousEnvironment;
        }
    }

    private static object NegateNumber(
        object? value,
        SourceSpan span)
    {
        if (TryGetInteger(value, out long integer))
        {
            return ExecuteCheckedInteger(
                () => checked(-integer),
                span);
        }

        if (TryGetFloatingPoint(value, out double floatingPoint))
        {
            return -floatingPoint;
        }

        throw new InvalidOperationException(
            $"Numeric negation received incompatible runtime value " +
            $"`{GetRuntimeTypeName(value)}`.");
    }

    private static object Add(
        object? left,
        object? right,
        SourceSpan span)
    {
        if (left is string leftString &&
            right is string rightString)
        {
            return leftString + rightString;
        }

        if (TryGetInteger(left, out long leftInteger) &&
            TryGetInteger(right, out long rightInteger))
        {
            return ExecuteCheckedInteger(
                () => checked(leftInteger + rightInteger),
                span);
        }

        if (TryGetNumber(left, out double leftNumber) &&
            TryGetNumber(right, out double rightNumber))
        {
            return leftNumber + rightNumber;
        }

        throw new InvalidOperationException(
            $"Addition received incompatible runtime values: " +
            $"`{GetRuntimeTypeName(left)}` and " +
            $"`{GetRuntimeTypeName(right)}`.");
    }

    private static object Subtract(
        object? left,
        object? right,
        SourceSpan span)
    {
        if (TryGetInteger(left, out long leftInteger) &&
            TryGetInteger(right, out long rightInteger))
        {
            return ExecuteCheckedInteger(
                () => checked(leftInteger - rightInteger),
                span);
        }

        if (TryGetNumber(left, out double leftNumber) &&
            TryGetNumber(right, out double rightNumber))
        {
            return leftNumber - rightNumber;
        }

        throw new InvalidOperationException(
            $"Subtraction received incompatible runtime values: " +
            $"`{GetRuntimeTypeName(left)}` and " +
            $"`{GetRuntimeTypeName(right)}`.");
    }

    private static object Multiply(
        object? left,
        object? right,
        SourceSpan span)
    {
        if (TryGetInteger(left, out long leftInteger) &&
            TryGetInteger(right, out long rightInteger))
        {
            return ExecuteCheckedInteger(
                () => checked(leftInteger * rightInteger),
                span);
        }

        if (TryGetNumber(left, out double leftNumber) &&
            TryGetNumber(right, out double rightNumber))
        {
            return leftNumber * rightNumber;
        }

        throw new InvalidOperationException(
            $"Multiplication received incompatible runtime values: " +
            $"`{GetRuntimeTypeName(left)}` and " +
            $"`{GetRuntimeTypeName(right)}`.");
    }

    private static object Divide(
        object? left,
        object? right,
        SourceSpan span)
    {
        if (!TryGetNumber(left, out double leftNumber) ||
            !TryGetNumber(right, out double rightNumber))
        {
            throw new InvalidOperationException(
                $"Division received incompatible runtime values: " +
                $"`{GetRuntimeTypeName(left)}` and " +
                $"`{GetRuntimeTypeName(right)}`.");
        }

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
        if (!TryGetInteger(left, out long leftNumber) ||
            !TryGetInteger(right, out long rightNumber))
        {
            throw new InvalidOperationException(
                $"Remainder received incompatible runtime values: " +
                $"`{GetRuntimeTypeName(left)}` and " +
                $"`{GetRuntimeTypeName(right)}`.");
        }

        if (rightNumber == 0)
        {
            throw new RuntimeFault(
                DiagnosticCodes.DivisionByZero,
                "Remainder by zero.",
                span,
                "The right operand of `%` must not be zero.");
        }

        return ExecuteCheckedInteger(
            () => checked(leftNumber % rightNumber),
            span);
    }

    private static int CompareNumbers(
        object? left,
        object? right)
    {
        if (TryGetInteger(left, out long leftInteger) &&
            TryGetInteger(right, out long rightInteger))
        {
            return leftInteger.CompareTo(rightInteger);
        }

        if (TryGetNumber(left, out double leftNumber) &&
            TryGetNumber(right, out double rightNumber))
        {
            return leftNumber.CompareTo(rightNumber);
        }

        throw new InvalidOperationException(
            $"Numeric comparison received incompatible runtime values: " +
            $"`{GetRuntimeTypeName(left)}` and " +
            $"`{GetRuntimeTypeName(right)}`.");
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
    
    private static bool TryGetInteger(
        object? value,
        out long result)
    {
        switch (value)
        {
            case long longValue:
                result = longValue;
                return true;

            case int intValue:
                result = intValue;
                return true;

            case short shortValue:
                result = shortValue;
                return true;

            case byte byteValue:
                result = byteValue;
                return true;

            default:
                result = 0;
                return false;
        }
    }

    private static bool TryGetFloatingPoint(
        object? value,
        out double result)
    {
        switch (value)
        {
            case double doubleValue:
                result = doubleValue;
                return true;

            case float floatValue:
                result = floatValue;
                return true;

            default:
                result = 0;
                return false;
        }
    }

    private static bool TryGetNumber(
        object? value,
        out double result)
    {
        if (TryGetFloatingPoint(value, out result))
        {
            return true;
        }

        if (TryGetInteger(value, out long integer))
        {
            result = integer;
            return true;
        }

        result = 0;
        return false;
    }

    private static string GetRuntimeTypeName(object? value)
    {
        return value?.GetType().FullName ?? "null";
    }
}