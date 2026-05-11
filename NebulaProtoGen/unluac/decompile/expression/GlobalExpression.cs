namespace unluac.decompile.expression
{
	public class GlobalExpression : Expression
	{
		private readonly string _name;
		private readonly int _index;

		public GlobalExpression(string name, int index) : base(PRECEDENCE_ATOMIC)
		{
			_name = name;
			_index = index;
		}

		public override int GetConstantIndex() => _index;
		public override bool IsDotChain() => true;
		public override void Print(Decompiler d, Output output) => output.Print(_name);
		public override bool IsBrief() => true;
		public override bool IsIdentifier() => true;
		public override string AsName() => _name;
	}
}
