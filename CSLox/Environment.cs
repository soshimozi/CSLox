using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSLox;

internal class Environment
{
    public readonly Environment? Enclosing;
    private readonly Dictionary<string, object?> _values = new Dictionary<string, object?>();
    public Environment(Environment? enclosing = null)
    {
        Enclosing = enclosing;
    }

    public object? Get(Token? name)
    {
        if (name == null) return null;
        if (_values.ContainsKey(name.Lexeme))
        {
            return _values[name.Lexeme];
        }

        if(Enclosing == null) throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");

        return Enclosing.Get(name);
    }

    public void Assign(Token? name, object? value)
    {
        if(name == null) return;
        if (_values.ContainsKey(name.Lexeme))
        {
            _values[name.Lexeme] = value;
            return;
        }

        if (Enclosing == null) throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
        Enclosing.Assign(name, value);
    }

    public void Define(string name, object? value)
    {
        _values.Put(name, value);
    }

    private Environment? Ancestor(int distance)
    {
        var environment = this;
        for (var i = 0; i < distance; i++)
        {
            environment = environment?.Enclosing;
        }

        return environment;;
    }

    public object? GetAt(int distance, string? name)
    {
        var dictionary = Ancestor(distance);
        return dictionary?._values.GetValueOrDefault(name ?? string.Empty);
    }

    public void AssignAt(int distance, Token? name, object? value)
    {
        if(name == null) return;
        Ancestor(distance)?._values.Put(name.Lexeme, value);
    }

    public override string ToString()
    {
        var result = new StringBuilder();
        result.Append(string.Join(',', _values.Values));
        if (Enclosing != null)
        {
            result.Append($" -> {Enclosing}");
        }

        return result.ToString();
    }
}