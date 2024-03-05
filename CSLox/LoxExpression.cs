namespace CSLox;

internal abstract class LoxExpression
{
    internal interface IVisitor<out TR>
    {
        TR VisitAssignExpression(LoxExpression.Assign expr);
        TR VisitThisExpression(LoxExpression.This expr);
        TR VisitGroupingExpression(LoxExpression.Grouping grouping);
        TR VisitSetExpression(Set set);
        TR VisitVariableExpression(Variable variable);
        TR VisitLogicalExpression(Logical logical);
        TR VisitBinaryExpression(Binary binary);
        TR VisitUnaryExpression(Unary unary);
        TR VisitCallExpression(Call call);
        TR VisitGetExpression(Get get);
        TR VisitLiteralExpression(Literal literal);
        TR VisitSuperExpression(Super super);
        TR VisitConstExpression(Const expression);
    }

    internal class Assign : LoxExpression
    {

        public readonly Token? Name;
        public readonly LoxExpression? Value;

        public Assign(Token? name, LoxExpression? value)
        {
            Name = name;
            Value = value;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitAssignExpression(this);
        }
    }

    internal class Variable : LoxExpression
    {
        public readonly Token? Name;
        public Variable(Token? name)
        {
            Name = name;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitVariableExpression(this);
        }

    }

    internal class Logical : LoxExpression
    {
        public readonly LoxExpression? Left;
        public readonly LoxExpression? Right;
        public readonly Token? Operator;

        public Logical(LoxExpression? left, Token? op, LoxExpression? right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitLogicalExpression(this);
        }
    }

    internal class Binary : LoxExpression
    {
        public readonly LoxExpression? Left;
        public readonly LoxExpression? Right;
        public readonly Token? Operator;
        public Binary(LoxExpression? left, Token? op, LoxExpression? right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitBinaryExpression(this);
        }
    }

    internal class Unary : LoxExpression
    {
        public readonly LoxExpression? Right;
        public readonly Token? Operator;
        public Unary(Token? op, LoxExpression? right)
        {
            Operator = op;
            Right = right;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitUnaryExpression(this);
        }
    }

    internal class Call : LoxExpression
    {
        public readonly LoxExpression? Callee;
        public readonly Token? Paren;
        public readonly List<LoxExpression?> Arguments;

        public Call(LoxExpression? callee, Token paren, List<LoxExpression?> arguments)
        {
            Callee = callee;
            Paren = paren;
            Arguments = arguments;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitCallExpression(this);
        }
    }

    internal abstract TR Accept<TR>(IVisitor<TR> visitor);

    internal class Get : LoxExpression
    {
        public readonly LoxExpression? Object;
        public readonly Token? Name;
        public Get(LoxExpression? obj, Token? name)
        {
            Object = obj;
            Name = name;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitGetExpression(this);
        }
    }

    internal class Literal : LoxExpression
    {
        public readonly object? Value;
        public Literal(object? value)
        {
            Value = value;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitLiteralExpression(this);
        }
    }


    public class Super : LoxExpression
    {
        public readonly Token? Keyword;
        public readonly Token? Method;
        public Super(Token? keyword, Token? method)
        {
            Keyword = keyword;
            Method = method;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitSuperExpression(this);
        }
    }

    public class This : LoxExpression
    {
        public readonly Token? Keyword;

        public This(Token? keyword)
        {
            Keyword = keyword;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitThisExpression(this);
        }
    }

    public class Grouping : LoxExpression
    {
        public readonly LoxExpression? Expression;
        public Grouping(LoxExpression? expr)
        {
            Expression = expr;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitGroupingExpression(this);
        }
    }

    public class Set : LoxExpression
    {
        public readonly LoxExpression? Object;
        public readonly Token? Name;
        public readonly LoxExpression? Value;
        public Set(LoxExpression? obj, Token? name, LoxExpression? value)
        {
            Object = obj;
            Name = name;
            Value = value;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitSetExpression(this);
        }
    }

    public class Const : LoxExpression
    {
        public readonly Token? Name;
        public Const(Token? name)
        {
            Name = name;
        }

        internal override TR Accept<TR>(IVisitor<TR> visitor)
        {
            return visitor.VisitConstExpression(this);
        }
    }
}

