namespace unluac.decompile.expression
{
	public class BinaryExpression : Expression
	{
		private readonly string _op;
		private readonly Expression _left;
		private readonly Expression _right;
		private readonly int _associativity;

		public BinaryExpression(string op, Expression left, Expression right, int precedence, int associativity)
		  : base(precedence)
		{
			_op = op;
			_left = left;
			_right = right;
			_associativity = associativity;
		}

		public override bool IsUngrouped() => !BeginsWithParen();

		public override int GetConstantIndex()
		{
			int l = _left.GetConstantIndex();
			int r = _right.GetConstantIndex();
			return l >= r ? l : r;
		}

		public override void Print(Decompiler d, Output output)
		{
			bool leftGroup = LeftNeedsGrouping();
			bool rightGroup = RightNeedsGrouping();
			if (leftGroup) output.Print("(");
			_left.Print(d, output);
			if (leftGroup) output.Print(")");
			output.Print(" ");
			output.Print(_op);
			output.Print(" ");
			if (rightGroup) output.Print("(");
			_right.Print(d, output);
			if (rightGroup) output.Print(")");
		}

		public override bool BeginsWithParen() => LeftNeedsGrouping() || _left.BeginsWithParen();

		private bool LeftNeedsGrouping()
		{
			return precedence > _left.precedence || (precedence == _left.precedence && _associativity == ASSOCIATIVITY_RIGHT);
		}

		private bool RightNeedsGrouping()
		{
			return precedence > _right.precedence || (precedence == _right.precedence && _associativity == ASSOCIATIVITY_LEFT);
		}
	}
}
