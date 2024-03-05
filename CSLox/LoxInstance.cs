using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSLox;

internal class LoxInstance
{
    private readonly LoxClass? _class;

    private readonly Dictionary<string, object?> _fields = new Dictionary<string, object?>();

    public LoxInstance(LoxClass? klass)
    {
        _class = klass;
    }

    public object? Get(Token? name)
    {
        if (name == null)
        {
            throw new RuntimeError(name, $"Undefined property '{name?.Lexeme}'");
        }

        if (_fields.ContainsKey(name.Lexeme))
        {
            return _fields[name.Lexeme];
        }

        var method = _class?.FindMethod(name.Lexeme);
        if(method == null) throw new RuntimeError(name, $"Undefined property '{name?.Lexeme}'");

        return method.Bind(this);
    }

    public void Set(Token? name, object? value)
    {
        if (name == null) return;
        _fields.Put(name.Lexeme, value);
    }

    public override string ToString()
    {
        return $"{_class?.Name} instance";
    }
}