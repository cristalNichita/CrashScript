using CrashScript.Language.Binding.Nodes;
using CrashScript.Language.Binding.Symbols;
using CrashScript.Language.Diagnostics;
using CrashScript.Language.Lexing;
using CrashScript.Language.Source;
using CrashScript.Language.Syntax;
using CrashScript.Language.Syntax.Expressions;
using CrashScript.Language.Syntax.Statements;

namespace CrashScript.Language.Binding;

public sealed class Binder
{
    private readonly DiagnosticBag _diagnostics = new();
    
    private readonly Dictionary<string, List<FunctionSymbol>>
        _functions = new(StringComparer.Ordinal);

    private readonly Dictionary<
        FunctionDeclarationStatementSyntax,
        FunctionSymbol> _declaredFunctions = [];
    
    private BoundScope _scope = new(parent: null);
    private FunctionSymbol? _currentFunction;

    public Binder()
    {
        foreach (FunctionSymbol function in BuiltinFunctions.All)
        {
            AddFunction(function);
        }
    }

    public BindResult Bind(CompilationUnitSyntax syntax)
    {
        ArgumentNullException.ThrowIfNull(syntax);
        
        DeclareTopLevelFunctions(syntax);

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
    
    private void DeclareTopLevelFunctions(
        CompilationUnitSyntax syntax)
    {
        foreach (FunctionDeclarationStatementSyntax declaration
                 in syntax.Statements
                     .OfType<FunctionDeclarationStatementSyntax>())
        {
            FunctionSymbol function =
                CreateFunctionSymbol(declaration);

            _declaredFunctions.Add(
                declaration,
                function);

            if (_functions.ContainsKey(function.Name))
            {
                _diagnostics.Report(
                    DiagnosticCodes.ProcessAlreadyDeclared,
                    DiagnosticCategory.Type,
                    $"Process `{function.Name}` is already declared.",
                    declaration.IdentifierToken.Span,
                    "CrashScript 0.1 does not support user-defined overloads.");

                continue;
            }

            AddFunction(function);
        }
    }
    
    private FunctionSymbol CreateFunctionSymbol(
        FunctionDeclarationStatementSyntax syntax)
    {
        var parameterNames =
            new HashSet<string>(StringComparer.Ordinal);

        var parameters =
            new List<ParameterSymbol>();

        foreach (ParameterSyntax parameterSyntax
                 in syntax.Parameters)
        {
            TypeSymbol parameterType =
                BindType(parameterSyntax.Type);

            if (parameterType == TypeSymbol.Void)
            {
                _diagnostics.Report(
                    DiagnosticCodes.InvalidVariableType,
                    DiagnosticCategory.Type,
                    "A process parameter cannot have type `void`.",
                    parameterSyntax.Type.Span);

                parameterType = TypeSymbol.Error;
            }

            if (!parameterNames.Add(parameterSyntax.Name))
            {
                _diagnostics.Report(
                    DiagnosticCodes.DuplicateParameter,
                    DiagnosticCategory.Type,
                    $"Parameter `{parameterSyntax.Name}` is declared more than once.",
                    parameterSyntax.IdentifierToken.Span);
            }

            parameters.Add(
                new ParameterSymbol(
                    parameterSyntax.Name,
                    parameterType));
        }

        TypeSymbol returnType =
            BindType(syntax.ReturnType);

        return new FunctionSymbol(
            syntax.Name,
            parameters.ToArray(),
            returnType,
            isNative: false);
    }
    
    private void AddFunction(
        FunctionSymbol function)
    {
        if (!_functions.TryGetValue(
                function.Name,
                out List<FunctionSymbol>? overloads))
        {
            overloads = [];
            _functions.Add(
                function.Name,
                overloads);
        }

        overloads.Add(function);
    }

    private IReadOnlyList<FunctionSymbol> FindFunctions(
        string name)
    {
        return _functions.TryGetValue(
            name,
            out List<FunctionSymbol>? functions)
            ? functions
            : [];
    }

    private BoundStatement BindStatement(
        StatementSyntax syntax)
    {
        return syntax switch
        {
            VariableDeclarationStatementSyntax declaration =>
                BindVariableDeclarationStatement(declaration),

            FunctionDeclarationStatementSyntax function =>
                BindFunctionDeclaration(function),

            ReturnStatementSyntax returnStatement =>
                BindReturnStatement(returnStatement),

            BlockStatementSyntax block =>
                BindBlockStatement(block),

            IfStatementSyntax ifStatement =>
                BindIfStatement(ifStatement),

            WhileStatementSyntax whileStatement =>
                BindWhileStatement(whileStatement),

            ExpressionStatementSyntax expressionStatement =>
                BindExpressionStatement(expressionStatement),

            _ => throw new InvalidOperationException(
                $"Unsupported statement syntax: {syntax.GetType().Name}")
        };
    }
    
    private BoundFunctionDeclaration BindFunctionDeclaration(
        FunctionDeclarationStatementSyntax syntax)
    {
        FunctionSymbol function;

        if (_declaredFunctions.TryGetValue(
                syntax,
                out FunctionSymbol? declaredFunction))
        {
            function = declaredFunction
                       ?? throw new InvalidOperationException(
                           $"Declared process symbol for `{syntax.Name}` is null.");
        }
        else
        {
            _diagnostics.Report(
                DiagnosticCodes.ProcessMustBeTopLevel,
                DiagnosticCategory.Type,
                $"Process `{syntax.Name}` must be declared at the top level.",
                syntax.ProcessKeyword.Span);

            function = CreateFunctionSymbol(syntax);
        }

        BoundScope previousScope = _scope;
        FunctionSymbol? previousFunction = _currentFunction;

        _scope = new BoundScope(previousScope);
        _currentFunction = function;

        try
        {
            foreach (ParameterSymbol parameter in function.Parameters)
            {
                _scope.TryDeclareVariable(parameter);
            }

            BoundStatement[] statements = syntax.Body.Statements
                .Select(BindStatement)
                .ToArray();

            var body = new BoundBlockStatement(
                statements,
                syntax.Body.Span);

            if (function.ReturnType != TypeSymbol.Void &&
                function.ReturnType != TypeSymbol.Error &&
                !AlwaysReturns(body))
            {
                _diagnostics.Report(
                    DiagnosticCodes.MissingReturn,
                    DiagnosticCategory.Type,
                    $"Process `{function.Name}` does not return a value on every path.",
                    syntax.IdentifierToken.Span,
                    $"Add `return` for type `{function.ReturnType.Name}`.");
            }

            return new BoundFunctionDeclaration(
                function,
                body,
                syntax.Span);
        }
        finally
        {
            _scope = previousScope;
            _currentFunction = previousFunction;
        }
    }
    
    private BoundReturnStatement BindReturnStatement(
        ReturnStatementSyntax syntax)
    {
        BoundExpression? expression =
            syntax.Expression is null
                ? null
                : BindExpression(syntax.Expression);

        if (_currentFunction is null)
        {
            _diagnostics.Report(
                DiagnosticCodes.ReturnOutsideProcess,
                DiagnosticCategory.Type,
                "`return` can only be used inside a process.",
                syntax.ReturnKeyword.Span);

            return new BoundReturnStatement(
                expression,
                syntax.Span);
        }

        if (_currentFunction.ReturnType == TypeSymbol.Void)
        {
            if (expression is not null)
            {
                _diagnostics.Report(
                    DiagnosticCodes.CannotReturnValue,
                    DiagnosticCategory.Type,
                    $"Process `{_currentFunction.Name}` returns `void` and cannot return a value.",
                    syntax.Expression!.Span,
                    "Use `return;` without an expression.");
            }

            return new BoundReturnStatement(
                expression,
                syntax.Span);
        }

        if (expression is null)
        {
            _diagnostics.Report(
                DiagnosticCodes.ReturnValueRequired,
                DiagnosticCategory.Type,
                $"Process `{_currentFunction.Name}` must return a value of type `{_currentFunction.ReturnType.Name}`.",
                syntax.ReturnKeyword.Span);

            return new BoundReturnStatement(
                null,
                syntax.Span);
        }

        BoundExpression convertedExpression =
            BindConversion(
                expression,
                _currentFunction.ReturnType,
                syntax.Expression!.Span);

        return new BoundReturnStatement(
            convertedExpression,
            syntax.Span);
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
    
    private BoundBlockStatement BindBlockStatement(
        BlockStatementSyntax syntax)
    {
        BoundScope previousScope = _scope;
        _scope = new BoundScope(previousScope);

        try
        {
            BoundStatement[] statements = syntax.Statements
                .Select(BindStatement)
                .ToArray();

            return new BoundBlockStatement(
                statements,
                syntax.Span);
        }
        finally
        {
            _scope = previousScope;
        }
    }
    
    private BoundIfStatement BindIfStatement(
        IfStatementSyntax syntax)
    {
        var branches = new List<BoundIfBranch>();

        foreach (IfBranchSyntax branchSyntax in syntax.Branches)
        {
            BoundExpression condition =
                BindBooleanCondition(
                    branchSyntax.Condition);

            BoundBlockStatement body =
                BindBlockStatement(
                    branchSyntax.Body);

            branches.Add(
                new BoundIfBranch(
                    condition,
                    body,
                    branchSyntax.Span));
        }

        BoundBlockStatement? elseBody = null;

        if (syntax.ElseClause is not null)
        {
            elseBody =
                BindBlockStatement(
                    syntax.ElseClause.Body);
        }

        return new BoundIfStatement(
            branches.ToArray(),
            elseBody,
            syntax.Span);
    }
    
    private BoundWhileStatement BindWhileStatement(
        WhileStatementSyntax syntax)
    {
        BoundExpression condition =
            BindBooleanCondition(
                syntax.Condition);

        BoundBlockStatement body =
            BindBlockStatement(
                syntax.Body);

        return new BoundWhileStatement(
            condition,
            body,
            syntax.Span);
    }
    
    private BoundExpression BindBooleanCondition(
        ExpressionSyntax syntax)
    {
        BoundExpression condition =
            BindExpression(syntax);

        if (condition.Type == TypeSymbol.Error)
        {
            return condition;
        }

        if (condition.Type == TypeSymbol.Bool)
        {
            return condition;
        }

        _diagnostics.Report(
            DiagnosticCodes.ConditionMustBeBoolean,
            DiagnosticCategory.Type,
            $"Condition must have type `bool`, but found `{condition.Type.Name}`.",
            syntax.Span,
            "CrashScript does not treat numbers, strings or null as booleans.");

        return new BoundErrorExpression(
            syntax.Span);
    }
    
    private BoundVariableDeclaration
        BindVariableDeclarationStatement(
            VariableDeclarationStatementSyntax syntax)
    {
        BoundExpression initializer =
            BindExpression(syntax.Initializer);

        TypeSymbol variableType;

        if (syntax.UsesTypeInference)
        {
            variableType = InferVariableType(
                syntax,
                initializer);
        }
        else
        {
            if (syntax.DeclaredType is null)
            {
                variableType = TypeSymbol.Error;
            }
            else
            {
                variableType =
                    BindType(syntax.DeclaredType);
            }

            if (variableType == TypeSymbol.Void)
            {
                _diagnostics.Report(
                    DiagnosticCodes.InvalidVariableType,
                    DiagnosticCategory.Type,
                    "Variables cannot have type `void`.",
                    syntax.DeclaredType?.Span ??
                    syntax.IdentifierToken.Span);

                variableType = TypeSymbol.Error;
            }

            initializer = BindConversion(
                initializer,
                variableType,
                syntax.Initializer.Span);
        }

        var variable = new VariableSymbol(
            syntax.Name,
            variableType,
            syntax.IsFixed);

        if (!_scope.TryDeclareVariable(variable))
        {
            _diagnostics.Report(
                DiagnosticCodes.VariableAlreadyDeclared,
                DiagnosticCategory.Type,
                $"Variable `{syntax.Name}` is already declared in this scope.",
                syntax.IdentifierToken.Span,
                "Use a different name or assign to the existing variable.");
        }

        return new BoundVariableDeclaration(
            variable,
            initializer,
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

            AssignmentExpressionSyntax assignment =>
                BindAssignmentExpression(assignment),

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
        VariableSymbol? variable =
            _scope.LookupVariable(syntax.Name);

        if (variable is not null)
        {
            return new BoundVariableExpression(
                variable,
                syntax.Span);
        }

        IReadOnlyList<FunctionSymbol> functions =
            FindFunctions(syntax.Name);

        if (functions.Count > 0)
        {
            _diagnostics.Report(
                DiagnosticCodes.FunctionMustBeCalled,
                DiagnosticCategory.Type,
                $"Function `{syntax.Name}` must be called with parentheses.",
                syntax.Span,
                $"Try `{syntax.Name}(...);`.");

            return new BoundErrorExpression(
                syntax.Span);
        }

        _diagnostics.Report(
            DiagnosticCodes.UndefinedName,
            DiagnosticCategory.Type,
            $"Name `{syntax.Name}` is not defined.",
            syntax.Span,
            "Declare the variable with `memory` or `fixed`.",
            "The computer refuses to invent missing variables.");

        return new BoundErrorExpression(
            syntax.Span);
    }
    
    private BoundExpression BindAssignmentExpression(
        AssignmentExpressionSyntax syntax)
    {
        BoundExpression value =
            BindExpression(syntax.Value);

        if (syntax.Target is not NameExpressionSyntax nameSyntax)
        {
            _diagnostics.Report(
                DiagnosticCodes.InvalidAssignmentTarget,
                DiagnosticCategory.Type,
                "The left side of an assignment must be a variable.",
                syntax.Target.Span);

            return new BoundErrorExpression(
                syntax.Span);
        }

        VariableSymbol? variable =
            _scope.LookupVariable(nameSyntax.Name);

        if (variable is null)
        {
            _diagnostics.Report(
                DiagnosticCodes.UndefinedName,
                DiagnosticCategory.Type,
                $"Variable `{nameSyntax.Name}` is not defined.",
                nameSyntax.Span,
                $"Declare it first with `memory {nameSyntax.Name} := ...;`.");

            return new BoundErrorExpression(
                syntax.Span);
        }

        if (variable.IsReadOnly)
        {
            _diagnostics.Report(
                DiagnosticCodes.CannotAssignFixed,
                DiagnosticCategory.Type,
                $"Cannot assign to fixed value `{variable.Name}`.",
                nameSyntax.Span,
                "`fixed` values cannot be changed after declaration.");

            return new BoundErrorExpression(
                syntax.Span);
        }

        BoundExpression convertedValue =
            BindConversion(
                value,
                variable.Type,
                syntax.Value.Span);

        if (convertedValue.Type == TypeSymbol.Error)
        {
            return new BoundErrorExpression(
                syntax.Span);
        }

        return new BoundAssignmentExpression(
            variable,
            convertedValue,
            syntax.Span);
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
        
        VariableSymbol? variable =
            _scope.LookupVariable(nameSyntax.Name);

        if (variable is not null)
        {
            _diagnostics.Report(
                DiagnosticCodes.ExpressionIsNotCallable,
                DiagnosticCategory.Type,
                $"Variable `{variable.Name}` cannot be called as a function.",
                nameSyntax.Span);

            return new BoundErrorExpression(
                syntax.Span);
        }

        IReadOnlyList<FunctionSymbol> candidates =
            FindFunctions(nameSyntax.Name);

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
    
    private BoundExpression BindConversion(
        BoundExpression expression,
        TypeSymbol targetType,
        SourceSpan diagnosticSpan)
    {
        if (expression.Type == TypeSymbol.Error ||
            targetType == TypeSymbol.Error)
        {
            return new BoundErrorExpression(
                diagnosticSpan);
        }

        Conversion conversion = Conversion.Classify(
            expression.Type,
            targetType);

        if (!conversion.Exists)
        {
            _diagnostics.Report(
                DiagnosticCodes.CannotConvertType,
                DiagnosticCategory.Type,
                $"Cannot convert type `{expression.Type.Name}` to `{targetType.Name}`.",
                diagnosticSpan);

            return new BoundErrorExpression(
                diagnosticSpan);
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
    
    private TypeSymbol InferVariableType(
        VariableDeclarationStatementSyntax syntax,
        BoundExpression initializer)
    {
        if (initializer.Type == TypeSymbol.Error)
        {
            return TypeSymbol.Error;
        }

        if (initializer.Type == TypeSymbol.Null)
        {
            _diagnostics.Report(
                DiagnosticCodes.CannotInferType,
                DiagnosticCategory.Type,
                $"Cannot infer the type of variable `{syntax.Name}` from `null`.",
                syntax.Initializer.Span,
                "Declare an explicit nullable type, for example `string?`.");

            return TypeSymbol.Error;
        }

        if (initializer.Type == TypeSymbol.Void)
        {
            _diagnostics.Report(
                DiagnosticCodes.CannotInferType,
                DiagnosticCategory.Type,
                $"Cannot infer the type of variable `{syntax.Name}` from a `void` expression.",
                syntax.Initializer.Span);

            return TypeSymbol.Error;
        }

        return initializer.Type;
    }
    
    private TypeSymbol BindType(TypeSyntax syntax)
    {
        TypeSymbol type = syntax.Name switch
        {
            "int" => TypeSymbol.Int,
            "float" => TypeSymbol.Float,
            "string" => TypeSymbol.String,
            "bool" => TypeSymbol.Bool,
            "void" => TypeSymbol.Void,
            _ => TypeSymbol.Error
        };

        if (type == TypeSymbol.Error)
        {
            _diagnostics.Report(
                DiagnosticCodes.UnknownType,
                DiagnosticCategory.Type,
                $"Type `{syntax.Name}` does not exist.",
                syntax.NameToken.Span);

            return TypeSymbol.Error;
        }

        if (!syntax.IsNullable)
        {
            return type;
        }

        if (type == TypeSymbol.Void)
        {
            _diagnostics.Report(
                DiagnosticCodes.InvalidVariableType,
                DiagnosticCategory.Type,
                "Type `void` cannot be nullable.",
                syntax.Span);

            return TypeSymbol.Error;
        }

        return TypeSymbol.Nullable(type);
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
    
    private static bool AlwaysReturns(
        BoundStatement statement)
    {
        return statement switch
        {
            BoundReturnStatement =>
                true,

            BoundBlockStatement block =>
                BlockAlwaysReturns(block),

            BoundIfStatement ifStatement =>
                IfAlwaysReturns(ifStatement),

            _ =>
                false
        };
    }

    private static bool BlockAlwaysReturns(
        BoundBlockStatement block)
    {
        foreach (BoundStatement statement in block.Statements)
        {
            if (AlwaysReturns(statement))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IfAlwaysReturns(
        BoundIfStatement statement)
    {
        if (statement.ElseBody is null)
        {
            return false;
        }

        if (!AlwaysReturns(statement.ElseBody))
        {
            return false;
        }

        return statement.Branches.All(branch =>
            AlwaysReturns(branch.Body));
    }

    private sealed record ApplicableFunction(
        FunctionSymbol Function,
        IReadOnlyList<Conversion> Conversions,
        int TotalCost);
}