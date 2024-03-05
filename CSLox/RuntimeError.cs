using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSLox;

internal class RuntimeError : Exception
{
    public readonly Token? Token;

    public RuntimeError(Token? token, string message) :base(message)
    {
        Token = token;
    }
}