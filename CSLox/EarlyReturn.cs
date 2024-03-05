using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSLox;

internal class EarlyReturn : Exception
{
    public readonly object? Value;
    public EarlyReturn(object? value)
    {
        Value = value;
    }
}