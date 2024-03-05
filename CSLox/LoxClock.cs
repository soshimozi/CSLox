using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSLox;

internal class LoxClock : ILoxCallable
{
    public int Arity()
    {
        return 0;
    }

    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        return (double)DateTime.Now.TimeOfDay.TotalMilliseconds / 1000;
    }

    public override string ToString()
    {
        return "<native fn>";
    }
}