using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CSLox;

internal class Resolver : LoxExpression.IVisitor<object?>, LoxStatement.IVisitor<object?>
{
    private readonly Interpreter _interpreter;
    //private readonly Stack<Dictionary<string, int>> _scopes = new Stack<Dictionary<string, int>>();
    private readonly List<Dictionary<string, bool>> _scopes = new List<Dictionary<string, bool>>();

    private FunctionType _currentFunction = FunctionType.None;
    private ClassType _currentClass = ClassType.None;

    public Resolver(Interpreter interpreter)
    {
        _interpreter = interpreter;
    }

    private enum FunctionType
    {
        None,
        /* Resolving and Binding function-type < Classes function-type-method
            FUNCTION
        */
        //> Classes function-type-method
        Function,
        //> function-type-initializer
        Initializer,
        //< function-type-initializer
        Method
        //< Classes function-type-method
    }
    //< function-type
    //> Classes class-type

    private enum ClassType
    {
        None,
        /* Classes class-type < Inheritance class-type-subclass
            CLASS
         */
        //> Inheritance class-type-subclass
        Class,
        Subclass
        //< Inheritance class-type-subclass
    }

    public void Resolve(List<LoxStatement?> statements)
    {
        foreach(var stmt in statements)
        {
            Resolve(stmt);
        }
    }

    public object? VisitAssignExpression(LoxExpression.Assign expr)
    {
        Resolve(expr.Value);
        ResolveLocal(expr, expr.Name);
        return null;
    }

    public object? VisitThisExpression(LoxExpression.This expr)
    {
        if (_currentClass != ClassType.None)
        {
            ResolveLocal(expr, expr.Keyword);
            return null;
        }

        Lox.Error(expr.Keyword, "Can't use 'this' outside of a class.");
        return null;
    }

    public object? VisitGroupingExpression(LoxExpression.Grouping grouping)
    {
        Resolve(grouping.Expression);
        return null;
    }

    public object? VisitSetExpression(LoxExpression.Set expression)
    {
        Resolve(expression.Value);
        Resolve(expression.Object);
        return null;
    }

    public object? VisitVariableExpression(LoxExpression.Variable expression)
    {
        if (expression.Name == null || _scopes.Count > 0 && _scopes[^1].GetValueOrDefault(expression.Name.Lexeme) == false)
        {
            Lox.Error(expression.Name, "Can't read local variable in it's own initializer.");
        }

        ResolveLocal(expression, expression.Name);
        return null;
    }

    public object? VisitLogicalExpression(LoxExpression.Logical logical)
    {
        Resolve(logical.Left);
        Resolve(logical.Right);
        return null;
    }

    public object? VisitBinaryExpression(LoxExpression.Binary expression)
    {
        Resolve(expression.Left);
        Resolve(expression.Right);
        return null;
    }

    public object? VisitUnaryExpression(LoxExpression.Unary expression)
    {
        Resolve(expression.Right);
        return null;
    }

    public object? VisitCallExpression(LoxExpression.Call expression)
    {
        Resolve(expression.Callee);

        foreach (var argument in expression.Arguments)
        {
            Resolve(argument);
        }

        return null;
    }

    public object? VisitGetExpression(LoxExpression.Get expression)
    {
        Resolve(expression.Object);
        return null;
    }

    public object? VisitLiteralExpression(LoxExpression.Literal literal)
    {
        return null;
    }

    public object? VisitSuperExpression(LoxExpression.Super expression)
    {
        if (_currentClass == ClassType.None)
        {
            Lox.Error(expression.Keyword, "Can't use 'super' outside of a class.");
        } else if (_currentClass != ClassType.Subclass)
        {
            Lox.Error(expression.Keyword, "Can't use 'super' in a class with no superclass.");
        }

        ResolveLocal(expression, expression.Keyword);
        return null;
    }

    public object? VisitConstExpression(LoxExpression.Const expression)
    {
        if (expression.Name == null || _scopes.Count > 0 && _scopes[^1].GetValueOrDefault(expression.Name.Lexeme) == false)
        {
            Lox.Error(expression.Name, "Can't read local variable in it's own initializer.");
        }

        ResolveLocal(expression, expression.Name);
        return null;
    }

    public object? VisitBlockStatement(LoxStatement.Block statement)
    {
        BeginScope();
        Resolve(statement);
        EndScope();
        return null;
    }

    public object? VisitClassStatement(LoxStatement.Class statement)
    {
        var enclosingClass = _currentClass;
        _currentClass = ClassType.Class;

        Declare(statement.Name);
        Define(statement.Name);

        if (statement.Name == null) return null;

        if (statement.Superclass?.Name != null && statement.Name.Lexeme.Equals(statement.Superclass.Name.Lexeme))
        {
            Lox.Error(statement.Superclass.Name, "A class can't inherit from itself");;
        }

        if (statement.Superclass != null)
        {
            _currentClass = ClassType.Subclass;
            Resolve(statement.Superclass);
        }

        if (statement.Superclass != null)
        {
            BeginScope();
            _scopes[^1].Put("super", true);
        }

        BeginScope();
        _scopes[^1].Put("this", true);

        foreach (var method in statement.Methods)
        {
            var declaration = FunctionType.Method;

            if (method.Name.Lexeme.Equals("init"))
            {
                declaration = FunctionType.Initializer;
            }

            ResolveFunction(method, declaration);
        }

        EndScope();

        if(statement.Superclass != null) EndScope();

        _currentClass = enclosingClass;
        return null;

    }

    public object? VisitExpressionStatement(LoxStatement.Expression statement)
    {
        Resolve(statement.Expr);
        return null;

    }

    public object? VisitFunctionStatement(LoxStatement.Function statement)
    {
        Declare(statement.Name);
        Define(statement.Name);

        ResolveFunction(statement, FunctionType.Function);

        return null;
    }

    public object? VisitIfStatement(LoxStatement.If statement)
    {
        Resolve(statement.Condition);
        Resolve(statement.ThenBranch);
        if(statement.ElseBranch != null) Resolve(statement.ElseBranch);
        return null;
    }

    public object? VisitWhileStatement(LoxStatement.While statement)
    {
        Resolve(statement.Condition);
        Resolve(statement.Body);
        return null;
    }

    public object? VisitVarStatement(LoxStatement.Var statement)
    {
        Declare(statement.Name);
        if(statement.Initializer != null)
            Resolve(statement.Initializer);

        Define(statement.Name);
        return null;
    }

    public object? VisitPrintStatement(LoxStatement.Print statement)
    {
        Resolve(statement.Expr);
        return null;
    }

    public object? VisitReturnStatement(LoxStatement.Return statement)
    {
        if (_currentFunction == FunctionType.None)
        {
            Lox.Error(statement.Keyword, "Can't return from top-level code.");
        }

        if (statement.Value == null) return null;

        if (_currentFunction == FunctionType.Initializer)
        {
            Lox.Error(statement.Keyword, "Can't return a value from an initializer.");
        }

        Resolve(statement.Value);

        return null;
    }

    public object? VisitConstStatement(LoxStatement.Const statement)
    {
        Declare(statement.Name);
        if (statement.Initializer == null)
        {
            Lox.Error(statement.Name, "Constant missing initializer.");
        }

        Resolve(statement.Initializer);
        return null;
    }

    private void Resolve(LoxStatement? stmt)
    {
        stmt?.Accept(this);
    }
    //< resolve-stmt
    //> resolve-expr
    private void Resolve(LoxExpression? expr)
    {
        expr?.Accept(this);
    }
    //< resolve-expr
    //> resolve-function
    /* Resolving and Binding resolve-function < Resolving and Binding set-current-function
      private void resolveFunction(Stmt.Function function) {
    */
    //> set-current-function
    private void ResolveFunction(
        LoxStatement.Function function, FunctionType type)
    {
        var enclosingFunction = _currentFunction;
        _currentFunction = type;

        //< set-current-function
        BeginScope();
        foreach (var param in function.Params)
        {
            Declare(param);
            Define(param);
        }
        Resolve(function.Body);
        EndScope();
        //> restore-current-function
        _currentFunction = enclosingFunction;
        //< restore-current-function
    }
    //< resolve-function
    //> begin-scope
    private void BeginScope()
    {
        _scopes.Add(new Dictionary<string, bool>());

    }
    //< begin-scope
    //> end-scope
    private void EndScope()
    {
        _scopes.RemoveAt(_scopes.Count - 1);
    }

    private void Declare(Token? name)
    {
        if (_scopes.Count == 0) return;
        if(name == null) return;

        var scope = Peek();

        //> duplicate-variable
        if (scope.ContainsKey(name.Lexeme))
        {
            Lox.Error(name,
                "Already a variable with this name in this scope.");
        }

        //< duplicate-variable
        scope[name.Lexeme] = false;
    }

    private Dictionary<string, bool> Peek()
    {
        return _scopes[^1];
    }

    //< declare
    //> define
    private void Define(Token? name)
    {
        if (_scopes.Count == 0) return;
        if(name == null) return;
        Peek()[name.Lexeme] = true;
    }

    //< define
    //> resolve-local
    private void ResolveLocal(LoxExpression? expr, Token? name)
    {
        if (name == null || expr == null) return;

        for (var i = _scopes.Count - 1; i >= 0; i--)
        {
            if (_scopes[i].ContainsKey(name.Lexeme))
            {
                _interpreter.Resolve(expr, _scopes.Count - 1 - i);
            }
        }
    }
}