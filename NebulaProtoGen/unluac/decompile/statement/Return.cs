using unluac.decompile.expression;

namespace unluac.decompile.statement
{
	public class Return : Statement
	{
		private readonly IList<Expression> _values; public Return(IList<Expression> values) { _values = values; }
		public override void Print(Decompiler d, Output output) { output.Print("do "); PrintTail(d, output); output.Print(" end"); }
		public override void PrintTail(Decompiler d, Output output) { output.Print("return"); if (_values.Count > 0) { output.Print(" "); expression.Expression.PrintSequence(d, output, _values, false, true); } }
	}
}
