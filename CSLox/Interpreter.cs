using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace CSLox;

internal class Interpreter : LoxExpression.IVisitor<object?>, LoxStatement.IVisitor<object?>
{
    private readonly Dictionary<LoxExpression, int?> _locals = new();
    private readonly Dictionary<LoxExpression, int?> _constants = new();
    private readonly Environment _globals = new();
    private Environment? _environment;

    public Interpreter()
    {
        _environment = _globals;
        //_globals.Define("clock", new LoxClock());
        //_globals.Define("current_state", 0);
    }

    public void AddGlobal(string name, object? value)
    {
        _globals.Define(name, value);
    }

    public void ExecuteBlock(List<LoxStatement?> statements,
        Environment? environment)
    {
        var previous = _environment;
        try
        {
            _environment = environment;

            foreach (var statement in statements)
            {
                Execute(statement);
            }
        }
        finally
        {
            _environment = previous;
        }
    }

    public void Interpret(List<LoxStatement?> statements)
    {
        try
        {
            foreach (var stmt in statements)
            {
                Execute(stmt);
            }
        }
        catch (RuntimeError re)
        {
            Lox.RuntimeError(re);
        }
    }

    public void Resolve(LoxExpression loxExpression, int depth)
    {
        _locals.Put(loxExpression, depth);
    }

    public object? VisitAssignExpression(LoxExpression.Assign expression)
    {
        if (IsConstantExpression(expression.Name, expression))
        {
            throw new RuntimeError(expression.Name, "You cannot assign a value to a constant.");
        }

        var value = Evaluate(expression.Value);

        var distance = _locals.GetValueOrDefault(expression);
        if (distance != null)
        {
            _environment?.AssignAt(distance.Value, expression.Name, value);
        }
        else
        {
            _globals.Assign(expression.Name, value);
        }

        return value;
    }

    public object? VisitThisExpression(LoxExpression.This expression)
    {
        return LookUpVariable(expression.Keyword, expression);
    }

    public object? VisitGroupingExpression(LoxExpression.Grouping expression)
    {
        return Evaluate(expression.Expression);
    }

    public object? VisitSetExpression(LoxExpression.Set expression)
    {
        var obj = Evaluate(expression.Object);
        if (obj is not LoxInstance instance)
        {
            throw new RuntimeError(expression.Name, "Only instances have fields.");
        }

        var value = Evaluate(expression.Value);
        instance.Set(expression.Name, value);
        return value;
    }

    public object? VisitVariableExpression(LoxExpression.Variable expression)
    {
        return LookUpVariable(expression.Name, expression);
    }

    public object? VisitLogicalExpression(LoxExpression.Logical expression)
    {
        var left = Evaluate(expression.Left);
        if (expression.Operator?.Type == TokenType.Or)
        {
            if (IsTruthy(left)) return left;
        }
        else
        {
            if (!IsTruthy(left)) return left;
        }

        return Evaluate(expression.Right);
    }

    public object? VisitBinaryExpression(LoxExpression.Binary expression)
    {
        var left = Evaluate(expression.Left);
        var right = Evaluate(expression.Right);

        switch (expression.Operator?.Type)
        {
            case TokenType.BangEqual: return !IsEqual(left, right);
            case TokenType.EqualEqual: return IsEqual(left, right);

            case TokenType.Greater:
                CheckNumberOperands(expression.Operator, left, right);
                // CheckNumberOperands throws an exception if the two operands are not double or are null
                return (double)left! > (double)right!;
            case TokenType.GreaterEqual:
                CheckNumberOperands(expression.Operator, left, right);
                return (double)left! >= (double)right!;
            case TokenType.Less:
                CheckNumberOperands(expression.Operator, left, right);
                return (double)left! < (double)right!;
            case TokenType.LessEqual:
                CheckNumberOperands(expression.Operator, left, right);
                return (double)left! <= (double)right!;
            case TokenType.Minus:
                CheckNumberOperands(expression.Operator, left, right);
                return (double)left! - (double)right!;
            case TokenType.Plus:
                switch (left)
                {
                        
                    case double d when right is double rightDouble:
                        return d + rightDouble;
                    case string when right is string:
                    {
                        var builder = new StringBuilder();
                        builder.Append(left);
                        builder.Append(right);
                        return builder.ToString();
                    }
                    case double when right is string:
                    case string when right is double:
                    {
                        var builder = new StringBuilder();
                        builder.Append(left);
                        builder.Append(right);
                        return builder.ToString();
                    }
                    default:
                        throw new RuntimeError(expression.Operator, "Operands must be numbers or strings.");
                }

            case TokenType.Slash:
                CheckNumberOperands(expression.Operator, left, right);
                return (double)left! / (double)right!;

            case TokenType.Star:
                CheckNumberOperands(expression.Operator, left, right);
                return (double)left! * (double)right!;
            default:
                return null;
        }

        // unreachable
    }

    public object? VisitUnaryExpression(LoxExpression.Unary expression)
    {
        var right = Evaluate(expression.Right);

        switch (expression.Operator?.Type)
        {
            case TokenType.Bang:
                return !IsTruthy(right);

            case TokenType.Minus:
                CheckNumberOperand(expression.Operator, right);
                return -(double)right!;
        }

        // unreachable
        return null;
    }

    public object? VisitCallExpression(LoxExpression.Call expression)
    {
        var callee = Evaluate(expression.Callee);
        var args = expression.Arguments.Select(Evaluate).ToList();

        if(callee is not ILoxCallable function)
        {
            throw new RuntimeError(expression.Paren, "Can only call functions and classes.");
        }

        if (args.Count != function.Arity())
        {
            throw new RuntimeError(expression.Paren,
                $"Expected {function.Arity()} arguments but got {args.Count}.");
        }

        return function.Call(this, args);
    }

    public object? VisitGetExpression(LoxExpression.Get expression)
    {
        var obj = Evaluate(expression.Object);
        if (obj is LoxInstance instance)
        {
            return instance.Get(expression.Name);
        }

        throw new RuntimeError(expression.Name, "Only instances have properties.");
    }

    public object? VisitLiteralExpression(LoxExpression.Literal expression)
    {
        return expression.Value;
    }

    public object? VisitSuperExpression(LoxExpression.Super expression)
    {
        var distance = _locals.GetValueOrDefault(expression);

        if (distance == null)
        {
            throw new RuntimeError(expression.Method, "Could not find super.");
        }
        var superClass = _environment?.GetAt(distance.Value, "super") as LoxClass;
        if (superClass == null) throw new RuntimeError(expression.Method, "Could not find super.");

        var obj = _environment?.GetAt(distance.Value - 1, "this");
        if (obj == null) throw new RuntimeError(expression.Method, "Could not find this.");

        if (obj is not LoxInstance loxInstance) throw new RuntimeError(expression.Method, "This pointer is not an instance.");

        if (expression.Method == null) throw new RuntimeError(expression.Method, "No method was supplied.");
        var method = superClass.FindMethod(expression.Method.Lexeme);

        if (method == null) throw new RuntimeError(expression.Method, $"Undefined property '{expression.Method.Lexeme}'.");
            
        return method.Bind(loxInstance);
    }

    public object? VisitConstExpression(LoxExpression.Const expression)
    {
        return LookUpVariable(expression.Name, expression);
    }

    public object? VisitBlockStatement(LoxStatement.Block statement)
    {
        ExecuteBlock(statement.Statements, new Environment(_environment));
        return null;
    }

    public object? VisitClassStatement(LoxStatement.Class statement)
    {
        object? superclass = null;
        if (statement.Superclass != null)
        {
            superclass = Evaluate(statement.Superclass);
            if (superclass is not LoxClass)
            {
                throw new RuntimeError(statement.Superclass.Name, "Superclass must be a class.");
            }
        }

        if(statement.Name != null)
            _environment?.Define(statement.Name.Lexeme, null);

        if (statement.Superclass != null)
        {
            _environment = new Environment(_environment);
            _environment.Define("super", superclass);
        }

        var methods = new Dictionary<string, LoxFunction>();
        foreach (var method in statement.Methods)
        {
            var function = new LoxFunction(method, _environment, method.Name.Lexeme == "init");
            methods.Put(method.Name.Lexeme, function);
        }

        if (statement.Name == null) return null;

        var klass = new LoxClass(statement.Name.Lexeme, superclass as LoxClass, methods);
        if (superclass != null)
        {
            _environment = _environment?.Enclosing;
        }

        _environment?.Assign(statement.Name, klass);
        return null;
    }

    public object? VisitExpressionStatement(LoxStatement.Expression statement)
    {
        Evaluate(statement.Expr);
        return null;
    }

    public object? VisitFunctionStatement(LoxStatement.Function statement)
    {
        var function = new LoxFunction(statement, _environment, false);
        _environment?.Define(statement.Name.Lexeme, function);
        return null;
    }

    public object? VisitIfStatement(LoxStatement.If statement)
    {
        if (IsTruthy(Evaluate(statement.Condition)))
        {
            Execute(statement.ThenBranch);
        } else if (statement.ElseBranch != null)
        {
            Execute(statement.ElseBranch);
        }

        return null;
    }

    public object? VisitWhileStatement(LoxStatement.While statement)
    {
        while (IsTruthy(Evaluate(statement.Condition)))
        {
            Execute(statement.Body);
        }

        return null;
    }

    public object? VisitVarStatement(LoxStatement.Var statement)
    {
        object? value = null;

        if (statement.Initializer != null)
        {
            value = Evaluate(statement.Initializer);
        }

        _environment?.Define(statement.Name.Lexeme, value);
        return null;
    }

    public object? VisitPrintStatement(LoxStatement.Print statement)
    {
        var value = Evaluate(statement.Expr);
        Console.WriteLine(Stringify(value));
        return null;
    }

    public object? VisitReturnStatement(LoxStatement.Return statement)
    {
        object? value = null;
        if(statement.Value != null) value = Evaluate(statement.Value);

        throw new EarlyReturn(value);
    }

    public object? VisitConstStatement(LoxStatement.Const statement)
    {
        var value = Evaluate(statement.Initializer);
        _environment?.Define(statement.Name.Lexeme, value);
        return null;
    }

    private object? Evaluate(LoxExpression? expr)
    {
        return expr?.Accept(this);
    }

    private void Execute(LoxStatement? statement)
    {
        statement?.Accept(this);
    }

    private bool IsConstantExpression(Token? name, LoxExpression expression)
    {
        return LookupConstant(name, expression) != null;
    }

    private object? LookUpVariable(Token? name, LoxExpression loxExpression)
    {
        var distance = _locals.GetValueOrDefault(loxExpression);
        return distance != null ? _environment?.GetAt(distance.Value, name?.Lexeme ?? string.Empty) : _globals.Get(name);
    }

    private object? LookupConstant(Token? name, LoxExpression expression)
    {
        var distance = _constants.GetValueOrDefault(expression);
        return distance != null
            ? _environment?.GetAt(distance.Value, name?.Lexeme ?? string.Empty)
            : _globals.Get(name);
    }

    //< Resolving and Binding look-up-variable
    //< Statements and State visit-variable
    //> check-operand
    private static void CheckNumberOperand(Token operatorToken, object? operand)
    {
        if (operand is double) return;
        throw new RuntimeError(operatorToken, "Operand must be a number.");
    }

    //< check-operand
    //> check-operands
    private static void CheckNumberOperands(Token operatorToken,
        object? left, object? right)
    {
        if (left is double && right is double) return;
        // [operand]
        throw new RuntimeError(operatorToken, "Operands must be numbers.");
    }
    //< check-operands
    //> is-truthy
    private static bool IsTruthy(object? obj)
    {
        return obj switch
        {
            null => false,
            bool b => b,
            _ => true
        };
    }
    //< is-truthy
    //> is-equal
    private static bool IsEqual(object? a, object? b)
    {
        return a switch
        {
            null when b == null => true,
            null => false,
            _ => a.Equals(b)
        };
    }

    //< is-equal
    //> stringify
    private static string Stringify(object? obj)
    {
        switch (obj)
        {
            case null:
                return "nil";
            case double:
            {
                var text = obj.ToString();

                if (text == null) return "nil";

                if (text.EndsWith(".0"))
                {
                    text = text[..^2];
                }
                return text;
            }
            default:
                return obj.ToString() ?? "nil";
        }
    }
}