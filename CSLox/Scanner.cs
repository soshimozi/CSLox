using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSLox;

internal class Scanner
{
    private static readonly Dictionary<string, TokenType> Keywords;
    private readonly string _source;
    private readonly List<Token> _tokens = new List<Token>();
    private int _start = 0;
    private int _current = 0;
    private int _line = 1;

    public Scanner(string source)
    {
        _source = source;
    }

    static Scanner()
    {
        Keywords = new Dictionary<string, TokenType>
        {
            { "and", TokenType.And },
            { "class", TokenType.Class },
            { "else", TokenType.Else },
            { "false", TokenType.False },
            { "for", TokenType.For },
            { "fun", TokenType.Fun },
            { "if", TokenType.If },
            { "nil", TokenType.Nil },
            { "or", TokenType.Or },
            { "print", TokenType.Print },
            { "return", TokenType.Return },
            { "super", TokenType.Super },
            { "this", TokenType.This },
            { "true", TokenType.True },
            { "var", TokenType.Var },
            { "while", TokenType.While },
            { "const", TokenType.Const}
        };
    }

    internal List<Token> ScanTokens()
    {
        _tokens.Clear();

        while (!IsAtEnd())
        {
            _start = _current;
            ScanToken();
        }

        _tokens.Add(new Token(TokenType.EOF,  "", null, _line));
        return _tokens;
    }

    private void ScanToken()
    {
        var c = Advance();
        switch (c)
        {
            case '(': AddToken(TokenType.LeftParen); break;
            case ')': AddToken(TokenType.RightParen); break;
            case '{': AddToken(TokenType.LeftBrace); break;
            case '}': AddToken(TokenType.RightBrace); break;
            case ',': AddToken(TokenType.Comma); break;
            case '.': AddToken(TokenType.Dot); break;
            case '-': AddToken(TokenType.Minus); break;
            case '+': AddToken(TokenType.Plus); break;
            case ';': AddToken(TokenType.Semicolon); break;
            case '*': AddToken(TokenType.Star); break;

            case '!': AddToken(Match('=') ? TokenType.BangEqual : TokenType.Bang); break;
            case '=': AddToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal); break;
            case '<': AddToken(Match('=') ? TokenType.LessEqual : TokenType.Less); break;
            case '>': AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater); break;


            // slash
            case '/':
                if (Match('/'))
                {
                    while (Peek() != '\n' && !IsAtEnd()) Advance();
                }
                else
                {
                    AddToken(TokenType.Slash);
                }

                break;

            // whitespace

            case ' ':
            case '\r':
            case '\t':
                // Ignore whitespace.
                break;

            case '\n':
                _line++;
                break;

            // string-start
            case '"': String(); break;

            //> char-error
            default:
                /* Scanning char-error < Scanning digit-start
                        Lox.error(line, "Unexpected character.");
                */
                //> digit-start
                if (IsDigit(c))
                {
                    Number();
                    //> identifier-start
                }
                else if (IsAlpha(c))
                {
                    Identifier();
                    //< identifier-start
                }
                else
                {
                    Lox.Error(_line, "Unexpected character.");
                }
                //< digit-start
                break;
        }
    }

    private void Identifier()
    {
        while (IsAlphaNumeric(Peek())) Advance();

        var text = _source.Substring(_start, _current - _start);
        if (!Keywords.TryGetValue(text, out var tokenType))
        {
            tokenType = TokenType.Identifier;
        }

        AddToken(tokenType);
    }
    private void Number()
    {
        while (IsDigit(Peek())) Advance();

        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            // Consume the '.'
            Advance();

            while(IsDigit(Peek())) Advance();
        }

        AddToken(TokenType.Number, ParseDouble(_source.Substring(_start, _current - _start)));
    }

    private static double ParseDouble(string text, double defaultValue = 0.0)
    {
        if (!double.TryParse(text, out var valueResult))
        {
            valueResult = defaultValue;
        }

        return valueResult;
    }
    private void String()
    {
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '\n') _line++;
            Advance();
        }

        if (IsAtEnd())
        {
            Lox.Error(_line, "Unterminated string constant.");
            return;
        }

        // the closing "
        Advance();
        var value = _source.Substring(_start + 1,  _current - _start - 2);
        AddToken(TokenType.String, value);
    }
    private bool Match(char expected)
    {
        if (IsAtEnd()) return false;
        if (_source[_current] != expected) return false;
        _current++;
        return true;
    }

    private char Peek() => IsAtEnd() ? '\0' : _source[_current];
    private char PeekNext() => _current + 1 >= _source.Length ? '\0' : _source[_current + 1];
    private static bool IsAlpha(char c) => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '_';
    private static bool IsAlphaNumeric(char c) => IsAlpha(c) || IsDigit(c);
    private static bool IsDigit(char ch) => ch is >= '0' and <= '9';
    private bool IsAtEnd() => _current >= _source.Length;
    private char Advance() => _source[_current++];
    private void AddToken(TokenType type, object? literal = null) => _tokens.Add(new Token(type, _source.Substring(_start, _current - _start), literal, _line));
}