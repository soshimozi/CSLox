using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSLox;

internal class LoxClass : ILoxCallable
{
    public readonly string Name;
    public readonly LoxClass? SuperClass;
    private readonly Dictionary<string, LoxFunction> _methods;

    public LoxClass(string name, LoxClass? superclass,
        Dictionary<string, LoxFunction> methods)
    {
        Name = name;
        SuperClass = superclass;
        _methods = methods;
    }

    public LoxFunction? FindMethod(string name)
    {
        return _methods.ContainsKey(name) ? _methods[name] : SuperClass?.FindMethod(name);
    }

    public override string ToString()
    {
        return Name;
    }

    public int Arity() => FindMethod("init")?.Arity() ?? 0;

    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        var instance = new LoxInstance(this);
        var initializer = FindMethod("init");
        initializer?.Bind(instance).Call(interpreter, arguments);

        return instance;
    }
}