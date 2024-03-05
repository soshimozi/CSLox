using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSLox;

internal static class Lox
{
    private static bool _hadError = false;
    private static bool _hadRuntimeEror = false;
    private static readonly Interpreter Interpreter = new Interpreter();

    static Lox()
    {
        Interpreter.AddGlobal("clock", new LoxClock());
        Interpreter.AddGlobal("current_state", 0);
        Interpreter.AddGlobal("last_state", 0);
    }

    public static void Run(string[] args)
    {
        switch (args.Length)
        {
            case > 1:
                Console.WriteLine("Usage: jlox [script file]");
                System.Environment.Exit(64);
                break;
            case 1:
                RunFile(args[0]);
                break;
            default: // should only be zero
                RunPrompt();
                break;
        }
    }

    private static void RunFile(string fileName)
    {
        var data = File.ReadAllText(fileName);
        Run(data);
    }

    private static void RunPrompt()
    {
        for (;;)
        {
            Console.Write("> ");
                
            var line = Console.ReadLine();
            if (string.IsNullOrEmpty(line)) break;
            Run(line);

            _hadError = false;
        }
    }

    private static void Run(string source)
    {
        var scanner = new Scanner(source);
        var tokens = scanner.ScanTokens();

        var parser = new Parser(tokens);
        var statements = parser.Parse();

        if (_hadError) return;

        var resolver = new Resolver(Interpreter);
        resolver.Resolve(statements);

        if (_hadError) return;

        Interpreter.Interpret(statements);
    }

    private static void Report(int line, string where,
        string message)
    {
        Console.WriteLine(
            $"[line {line}] Error{where}: {message}");
        _hadError = true;
    }
    //< lox-error
    //> Parsing Expressions token-error
    internal static void Error(Token? token, string message) => Report(token?.Line ?? 0, token?.Type == TokenType.EOF ? " at end" : $" at '{token?.Lexeme}'", message);
    internal static void Error(int line, string message) => Report(line, "", message);

    internal static void RuntimeError(RuntimeError re)
    {
        Console.WriteLine($"{re.Message}\n[line {re.Token?.Line}]");
        _hadRuntimeEror = true;
    }
}