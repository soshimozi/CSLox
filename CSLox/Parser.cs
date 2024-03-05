using System;
using System.Diagnostics;

namespace CSLox;

internal class Parser
{
    private readonly List<Token> _tokens;
    private int _current = 0;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public List<LoxStatement?> Parse()
    {
        var statements = new List<LoxStatement?>();
        while(!IsAtEnd())
        {
            statements.Add(Declaration());
        }

        return statements;
    }

    private LoxExpression? Expression()
    {
        return Assignment();
    }
        
    private LoxStatement? Declaration()
    {
        try
        {
            if (Match(TokenType.Class)) return ClassDeclaration();
            if (Match(TokenType.Fun)) return Function("function");
            if (Match(TokenType.Const)) return ConstDeclaration();
            return Match(TokenType.Var) ? VarDeclaration() : Statement();
        }
        catch(ParseError _)
        {
            Synchronize();
            return null;
        }
    }

    private LoxStatement Statement()
    {
        if(Match(TokenType.For)) return ForStatement();
        if(Match(TokenType.If)) return IfStatement();
        if(Match(TokenType.While)) return WhileStatement();
        if(Match(TokenType.Return)) return ReturnStatement();
        if(Match(TokenType.Print)) return PrintStatement();

        return Match(TokenType.LeftBrace) ? new LoxStatement.Block(Block()) : ExpressionStatement();
    }

    private LoxStatement ForStatement()
    {
        Consume(TokenType.LeftParen, "Expect '(' after 'for'.");

        LoxStatement? initializer;
        if (Match(TokenType.Semicolon))
        {
            initializer = null;
        } else if (Match(TokenType.Var))
        {
            initializer = VarDeclaration();
        }
        else
        {
            initializer = ExpressionStatement();
        }

        LoxExpression? condition = null;
        if (!Check(TokenType.Semicolon))
        {
            condition = Expression();
        }

        Consume(TokenType.Semicolon, "Expect ';' after loop condition.");

        LoxExpression? increment = null;
        if (!Check(TokenType.RightParen))
        {
            increment = Expression();
        }

        Consume(TokenType.RightParen, "Expect ')' after for clauses.");

        var body = Statement(); // for(;;)

        // de-sugar increment
        if (increment != null) body = new LoxStatement.Block(new List<LoxStatement?> { body, new LoxStatement.Expression(increment)});

        //condition ??= new LoxExpression.Literal(true);

        // de-sugar condition
        body = new LoxStatement.While(condition ?? new LoxExpression.Literal(true), body);

        // de-sugar initializer
        if (initializer != null)
        {
            body = new LoxStatement.Block(new List<LoxStatement?> { initializer, body });
        }

        return body;
    }

    private LoxStatement IfStatement()
    {
        Consume(TokenType.LeftParen, "Expect '(' after 'if'.");
        var condition = Expression();
        Consume(TokenType.RightParen, "Expect ')' after if condition.");

        var thenBranch = Statement();
        LoxStatement? elseBranch = null;
        if (Match(TokenType.Else))
        {
            elseBranch = Statement();
        }

        return new LoxStatement.If(condition, thenBranch, elseBranch);
    }

    private LoxStatement PrintStatement()
    {
        var value = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after value.");
        return new LoxStatement.Print(value);
    }


    private LoxStatement ReturnStatement()
    {
        var keyword = Previous();
        LoxExpression? value = null;
        if (!Check(TokenType.Semicolon))
        {
            value = Expression();
        }

        Consume(TokenType.Semicolon, "Expect ';' after return ");

        return new LoxStatement.Return(keyword, value);
    }

    private LoxStatement WhileStatement()
    {
        Consume(TokenType.LeftParen, "Expect '(' after while.");
        var condition = Expression();
        Consume(TokenType.RightParen, "Expect ')' after expression.");
        var body = Statement();

        return new LoxStatement.While(condition, body);
    }

    private LoxStatement ExpressionStatement()
    {
        var expr = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after expression.");
        return new LoxStatement.Expression(expr);
    }

    private LoxStatement ClassDeclaration()
    {
        var name = Consume(TokenType.Identifier, "Expect class name.");

        LoxExpression.Variable superclass = null;
        if (Match(TokenType.Less))
        {
            Consume(TokenType.Identifier, "Expect superclass name.");
            superclass = new LoxExpression.Variable(Previous());
        }

        Consume(TokenType.LeftBrace, "Expect '{' before class body.");
        var methods = new List<LoxStatement.Function>();
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            methods.Add(Function("method"));
        }

        Consume(TokenType.RightBrace, "Expect '}' after class body.");

        return new LoxStatement.Class(name, superclass, methods);
    }

    private LoxStatement ConstDeclaration()
    {
        var name = Consume(TokenType.Identifier, "Expect variable name.");
        LoxExpression? initializer = null;

        Consume(TokenType.Equal, "Expected = after Const declaration.");
        initializer = Expression();

        if (initializer == null)
        {
            Error(Peek(), "Expected initializer after Const declaration.");
        }

        Consume(TokenType.Semicolon, "Expect ';' after variable declaration.");
        return new LoxStatement.Const(name, initializer);
    }

    private LoxStatement VarDeclaration()
    {
        var name = Consume(TokenType.Identifier, "Expect variable name.");
        LoxExpression? initializer = null;

        if(Match(TokenType.Equal))
        {
            initializer = Expression();
        }

        Consume(TokenType.Semicolon, "Expect ';' after variable declaration.");
        return new LoxStatement.Var(name, initializer);
    }

    private LoxStatement.Function Function(string kind)
    {
        var name = Consume(TokenType.Identifier, $"Expect {kind} name.");
        Consume(TokenType.LeftParen, $"Expect '(' after {kind} name.");
        var parameters = new List<Token>();
        if (!Check(TokenType.RightParen))
        {
            do
            {
                if (parameters.Count >= 255)
                {
                    Error(Peek(), "Can't have more thant 255 parameters.");
                }

                parameters.Add(Consume(TokenType.Identifier, "Expect parameter name."));
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightParen, "Expect ')' after parameters.");

        Consume(TokenType.LeftBrace, $"Expect '{{' before {kind} body.");
        var body = Block();
        return new LoxStatement.Function(name, parameters, body);
    }

    private List<LoxStatement?> Block()
    {
        var statements = new List<LoxStatement?>();

        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            statements.Add(Declaration());
        }

        Consume(TokenType.RightBrace, "Expected '}' after block.");
        return statements;
    }

    private LoxExpression? Assignment()
    {
        var expr = Or();

        if (!Match(TokenType.Equal)) return expr;

        var equals = Previous();
        var value = Assignment();

        switch (expr)
        {
            case LoxExpression.Variable variable:
            {
                var name = variable.Name;
                return new LoxExpression.Assign(name, value);
            }
            case LoxExpression.Get expressionGet:
            {
                var get = expressionGet;
                return new LoxExpression.Set(get.Object, get.Name, value);
            }
            default:
                Error(equals, "Invalid assignment target.");
                return expr;
        }
    }

    private LoxExpression? Or()
    {
        var expr = And();

        while(Match(TokenType.Or))
        {
            var oper = Previous();
            var right = And();
            expr = new LoxExpression.Logical(expr, oper, right);
        }

        return expr;
    }

    private LoxExpression? And()
    {
        var expr = Equality();

        while(Match(TokenType.And))
        {
            var op = Previous();
            var right = Equality();
            expr = new LoxExpression.Logical(expr, op, right);
        }

        return expr;
    }

    private LoxExpression? Equality()
    {
        var expr = Comparison();
        while(Match(TokenType.BangEqual, TokenType.EqualEqual))
        {
            var op = Previous();
            var right = Comparison();
            expr = new LoxExpression.Binary(expr, op, right);
        }

        return expr;
    }

    private LoxExpression? Comparison()
    {
        var expr = Term();
        while(Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
        {
            var op = Previous();
            var right = Term();
            expr = new LoxExpression.Binary(expr, op, right);
        }

        return expr;
    }

    private LoxExpression? Term()
    {
        var expr = Factor();
        while(Match(TokenType.Minus, TokenType.Plus))
        {
            var oper = Previous();
            var right = Factor();
            expr = new LoxExpression.Binary(expr, oper, right);
        }

        return expr;
    }

    private LoxExpression? Factor()
    {
        var expr = Unary();
        while(Match(TokenType.Slash, TokenType.Star))
        {
            var operation = Previous();
            var right = Unary();
            expr = new LoxExpression.Binary(expr, operation, right);
        }

        return expr;
    }

    private LoxExpression? Unary()
    {
        if (!Match(TokenType.Bang, TokenType.Minus)) return Call();
        var operation = Previous();
        var right = Unary();
        return new LoxExpression.Unary(operation, right);

    }

    private LoxExpression? FinishCall(LoxExpression? callee)
    {
        var arguments = new List<LoxExpression?>();

        if(!Check(TokenType.RightParen))
        {
            do
            {
                if (arguments.Count >= 255)
                {
                    Error(Peek(), "Can't have more than 255 arguments.");
                }

                arguments.Add(Expression());
            } while (Match(TokenType.Comma));
        }

        var paren = Consume(TokenType.RightParen, "Expect ')' after arguments.");
        return new LoxExpression.Call(callee, paren, arguments);
    }

    private LoxExpression? Call()
    {
        var expr = Primary();
        while (true)
        {
            if (Match(TokenType.LeftParen))
            {
                expr = FinishCall(expr);
            } else if (Match(TokenType.Dot))
            {
                var name = Consume(TokenType.Identifier, "Expect property name after '.'.");
                expr = new LoxExpression.Get(expr, name);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private LoxExpression? Primary()
    {
        if (Match(TokenType.False)) return new LoxExpression.Literal(false);
        if(Match(TokenType.True)) return new LoxExpression.Literal(true);
        if(Match(TokenType.Nil)) return new LoxExpression.Literal(null);

        if (Match(TokenType.Number, TokenType.String))
        {
            return new LoxExpression.Literal(Previous().Literal);
        }

        if (Match(TokenType.Super))
        {
            var keyword = Previous();
            var method = Consume(TokenType.Dot, "Expect '.' after 'super'.");
            return new LoxExpression.Super(keyword, method);
        }

        if (Match(TokenType.This))
        {
            return new LoxExpression.This(Previous());
        }

        if (Match(TokenType.Identifier))
        {
            return new LoxExpression.Variable(Previous());
        }

        if (!Match(TokenType.LeftParen)) throw Error(Peek(), "Expect expression.");

        var expr = Expression();
        Consume(TokenType.RightParen, "Expect ')' after expression.");
        return new LoxExpression.Grouping(expr);
    }

    private bool Match(params TokenType[] tokenTypes)
    {
        if (!tokenTypes.Any(Check)) return false;

        Advance();
        return true;
    }

    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        throw Error(Peek(), message);
    }

    private bool Check(TokenType type)
    {
        if (IsAtEnd()) return false;
        return Peek().Type == type;
    }

    private Token Advance()
    {
        if (!IsAtEnd()) _current++;
        return Previous();
    }

    private bool IsAtEnd()
    {
        return Peek().Type == TokenType.EOF;
    }

    private Token Peek()
    {
        return _tokens[_current];
    }

    private Token Previous()
    {
        return _tokens[_current - 1];
    }

    private ParseError Error(Token token, string message)
    {
        Lox.Error(token, message);
        return new ParseError();
    }

    private void Synchronize()
    {
        Advance();

        while(!IsAtEnd())
        {
            if (Previous().Type == TokenType.Semicolon) return;

            switch(Peek().Type)
            {
                case TokenType.Class:
                case TokenType.Fun:
                case TokenType.Var:
                case TokenType.For:
                case TokenType.If:
                case TokenType.While:
                case TokenType.Print:
                case TokenType.Return:
                case TokenType.Const:
                    return;
            }

            Advance();
        }    
    }
}