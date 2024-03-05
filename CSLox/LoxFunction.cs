using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSLox;

internal class LoxFunction : ILoxCallable
{
    private readonly LoxStatement.Function _declaration;
    private readonly Environment? _closure;
    private readonly bool _isInitializer;

    public LoxFunction(LoxStatement.Function declaration, Environment? closure, bool isInitializer)
    {
        _declaration = declaration;
        _closure = closure;
        _isInitializer = isInitializer;
    }

    public LoxFunction Bind(LoxInstance instance)
    {
        var environment = new Environment(_closure);
        environment.Define("this", instance);

        return new LoxFunction(_declaration, environment, _isInitializer);
    }

    public int Arity()
    {
        return _declaration.Params.Count;
    }

    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        var environment = new Environment(_closure);

        for (var i = 0; i < _declaration.Params.Count; i++)
        {
            environment.Define(_declaration.Params[i].Lexeme, arguments[i]);
        }

        try
        {
            interpreter.ExecuteBlock(_declaration.Body, environment);
        }
        catch (EarlyReturn returnValue)
        {
            return _isInitializer ? _closure?.GetAt(0, "this") : returnValue.Value;
        }

        return _isInitializer ? _closure?.GetAt(0, "this") : null;
    }

    public override string ToString()
    {
        return $"<fn {_declaration.Name.Lexeme}>";
    }
}