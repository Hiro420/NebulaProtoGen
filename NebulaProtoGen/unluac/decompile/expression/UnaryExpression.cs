namespace unluac.decompile.expression
{
	public class UnaryExpression : Expression
	{
		private readonly string _op;
		private readonly Expression _expression;

		public UnaryExpression(string op, Expression expression, int precedence)
		  : base(precedence)
		{
			_op = op;
			_expression = expression;
		}

		public override bool IsUngrouped() => true;

		public override int GetConstantIndex() => _expression.GetConstantIndex();

		public override void Print(Decompiler d, Output output)
		{
			output.Print(_op);
			bool needGroup = precedence > _expression.precedence;
			if (needGroup) output.Print("(");
			_expression.Print(d, output);
			if (needGroup) output.Print(")");
		}
	}
}
