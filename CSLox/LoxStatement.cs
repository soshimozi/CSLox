using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;


namespace CSLox;

internal abstract class LoxStatement
{
    internal interface IVisitor<out TR>
    {
        TR VisitBlockStatement(Block statement);
        TR VisitClassStatement(Class statement);
        TR VisitExpressionStatement(Expression statement);
        TR VisitFunctionStatement(Function statement);
        TR VisitIfStatement(If statement);
        TR VisitWhileStatement(While statement);
        TR VisitVarStatement(Var statement);
        TR VisitPrintStatement(Print statement);
        TR VisitReturnStatement(Return statement);
        TR VisitConstStatement(Const statement);
    }

    internal class Block : LoxStatement
    {
        public readonly List<LoxStatement?> Statements;

        public Block(List<LoxStatement?> statements)
        {
            Statements = statements;
        }


        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitBlockStatement(this);
        }
    }

    internal class Class : LoxStatement
    {
        public readonly Token? Name;
        public readonly LoxExpression.Variable? Superclass;
        public readonly List<LoxStatement.Function> Methods;

        public Class(Token? name, LoxExpression.Variable? superclass, List<LoxStatement.Function> methods)
        {
            Name = name;
            Superclass = superclass;
            Methods = methods;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitClassStatement(this);
        }
    }

    internal class Expression : LoxStatement
    {
        public readonly LoxExpression? Expr;

        public Expression(LoxExpression? expr)
        {
            Expr = expr;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitExpressionStatement(this);
        }
    }

    internal class Function : LoxStatement
    {
        public readonly Token Name;
        public readonly List<Token> Params;
        public readonly List<LoxStatement?> Body;

        public Function(Token name, List<Token> parameters, List<LoxStatement?> body)
        {
            Name = name;
            Params = parameters;
            Body = body;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitFunctionStatement(this);
        }
    }

    internal class If : LoxStatement
    {
        public readonly LoxExpression? Condition;
        public readonly LoxStatement ThenBranch;
        public readonly LoxStatement? ElseBranch;

        public If(LoxExpression? condition, LoxStatement thenBranch, LoxStatement? elseBranch)
        {
            Condition = condition;
            ThenBranch = thenBranch;
            ElseBranch = elseBranch;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitIfStatement(this);
        }
    }

    internal class Return : LoxStatement
    {
        public readonly Token Keyword;
        public readonly LoxExpression? Value;

        public Return(Token keyword, LoxExpression? value)
        {
            Keyword = keyword;
            Value = value;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitReturnStatement(this);
        }
    }

    internal class While : LoxStatement
    {
        public readonly LoxExpression? Condition;
        public readonly LoxStatement Body;

        public While(LoxExpression? condition, LoxStatement body)
        {
            Condition = condition;
            Body = body;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitWhileStatement(this);
        }
    }

    internal class Const : LoxStatement
    {
        public readonly Token Name;
        public readonly LoxExpression? Initializer;

        public Const(Token name, LoxExpression? initializer)
        {
            Name = name;
            Initializer = initializer;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitConstStatement(this);
        }

    }

    internal class Var : LoxStatement
    {
        public readonly Token Name;
        public readonly LoxExpression? Initializer;

        public Var(Token name, LoxExpression? initializer)
        {
            Name = name;
            Initializer = initializer;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitVarStatement(this);
        }
    }

    internal class Print : LoxStatement
    {
        public readonly LoxExpression? Expr;

        public Print(LoxExpression? expression)
        {
            Expr = expression;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitPrintStatement(this);
        }
    }

    internal abstract TR Accept<TR>(IVisitor<TR> visitor);
}




