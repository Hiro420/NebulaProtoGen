using unluac.decompile.target;
using unluac.parse;

namespace unluac.decompile.expression
{
	public abstract class Expression
	{
		public const int PRECEDENCE_OR = 1;
		public const int PRECEDENCE_AND = 2;
		public const int PRECEDENCE_COMPARE = 3;
		public const int PRECEDENCE_BOR = 4;
		public const int PRECEDENCE_BXOR = 5;
		public const int PRECEDENCE_BAND = 6;
		public const int PRECEDENCE_SHIFT = 7;
		public const int PRECEDENCE_CONCAT = 8;
		public const int PRECEDENCE_ADD = 9;
		public const int PRECEDENCE_MUL = 10;
		public const int PRECEDENCE_UNARY = 11;
		public const int PRECEDENCE_POW = 12;
		public const int PRECEDENCE_ATOMIC = 13;

		public const int ASSOCIATIVITY_NONE = 0;
		public const int ASSOCIATIVITY_LEFT = 1;
		public const int ASSOCIATIVITY_RIGHT = 2;

		public static readonly Expression NIL = new ConstantExpression(new Constant(LNil.NIL), -1);

		public readonly int precedence;

		protected Expression(int precedence)
		{
			this.precedence = precedence;
		}

		public abstract void Print(Decompiler d, Output output);

		public virtual void PrintBraced(Decompiler d, Output output) => Print(d, output);
		public virtual void PrintMultiple(Decompiler d, Output output) => Print(d, output);
		public abstract int GetConstantIndex();

		public virtual bool BeginsWithParen() => false;
		public virtual bool IsMultiple() => false;
		public virtual bool IsNil() => false;
		public virtual bool IsClosure() => false;
		public virtual bool IsConstant() => false;
		public virtual bool IsUngrouped() => false;
		public virtual bool IsBoolean() => false;
		public virtual bool IsInteger() => false;
		public virtual int AsInteger() => throw new System.InvalidOperationException();
		public virtual bool IsString() => false;
		public virtual bool IsIdentifier() => false;
		public virtual bool IsDotChain() => false;
		public virtual int ClosureUpvalueLine() => throw new System.InvalidOperationException();
		public virtual void PrintClosure(Decompiler d, Output output, Target name) => throw new System.InvalidOperationException();
		public virtual string AsName() => throw new System.InvalidOperationException();
		public virtual bool IsTableLiteral() => false;
		public virtual bool IsNewEntryAllowed() => throw new System.InvalidOperationException();
		public virtual void AddEntry(TableLiteral.Entry entry) => throw new System.InvalidOperationException();
		public virtual bool IsMemberAccess() => false;
		public virtual Expression GetTable() => throw new System.InvalidOperationException();
		public virtual string GetField() => throw new System.InvalidOperationException();
		public virtual bool IsBrief() => false;
		public virtual bool IsEnvironmentTable(Decompiler d) => false;

		public static void PrintSequence(Decompiler d, Output output, IList<Expression> exprs, bool linebreak, bool multiple)
		{
			int n = exprs.Count;
			for (int i = 0; i < n; i++)
			{
				var expr = exprs[i];
				bool last = (i == n - 1) || expr.IsMultiple();
				if (last)
				{
					if (multiple) expr.PrintMultiple(d, output); else expr.Print(d, output);
					break;
				}
				expr.Print(d, output);
				output.Print(",");
				if (linebreak) output.Println(); else output.Print(" ");
			}
		}

		// Static factory helpers
		public static Expression makeADD(Expression a, Expression b) => new BinaryExpression("+", a, b, PRECEDENCE_ADD, ASSOCIATIVITY_LEFT);
		public static Expression makeSUB(Expression a, Expression b) => new BinaryExpression("-", a, b, PRECEDENCE_ADD, ASSOCIATIVITY_LEFT);
		public static Expression makeMUL(Expression a, Expression b) => new BinaryExpression("*", a, b, PRECEDENCE_MUL, ASSOCIATIVITY_LEFT);
		public static Expression makeDIV(Expression a, Expression b) => new BinaryExpression("/", a, b, PRECEDENCE_MUL, ASSOCIATIVITY_LEFT);
		public static Expression makeMOD(Expression a, Expression b) => new BinaryExpression("%", a, b, PRECEDENCE_MUL, ASSOCIATIVITY_LEFT);
		public static Expression makePOW(Expression a, Expression b) => new BinaryExpression("^", a, b, PRECEDENCE_POW, ASSOCIATIVITY_RIGHT);
		public static Expression makeIDIV(Expression a, Expression b) => new BinaryExpression("//", a, b, PRECEDENCE_MUL, ASSOCIATIVITY_LEFT);
		public static Expression makeBAND(Expression a, Expression b) => new BinaryExpression("&", a, b, PRECEDENCE_BAND, ASSOCIATIVITY_LEFT);
		public static Expression makeBOR(Expression a, Expression b) => new BinaryExpression("|", a, b, PRECEDENCE_BOR, ASSOCIATIVITY_LEFT);
		public static Expression makeBXOR(Expression a, Expression b) => new BinaryExpression("~", a, b, PRECEDENCE_BXOR, ASSOCIATIVITY_LEFT);
		public static Expression makeSHL(Expression a, Expression b) => new BinaryExpression("<<", a, b, PRECEDENCE_SHIFT, ASSOCIATIVITY_LEFT);
		public static Expression makeSHR(Expression a, Expression b) => new BinaryExpression(">>", a, b, PRECEDENCE_SHIFT, ASSOCIATIVITY_LEFT);
		public static Expression makeUNM(Expression a) => new UnaryExpression("-", a, PRECEDENCE_UNARY);
		public static Expression makeNOT(Expression a) => new UnaryExpression("not ", a, PRECEDENCE_UNARY);
		public static Expression makeLEN(Expression a) => new UnaryExpression("#", a, PRECEDENCE_UNARY);
		public static Expression makeBNOT(Expression a) => new UnaryExpression("~", a, PRECEDENCE_UNARY);
		public static Expression makeCONCAT(Expression left, Expression right) => new BinaryExpression("..", left, right, PRECEDENCE_CONCAT, ASSOCIATIVITY_RIGHT);
	}
}
