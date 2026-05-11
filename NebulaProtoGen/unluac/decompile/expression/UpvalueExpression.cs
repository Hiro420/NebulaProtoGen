namespace unluac.decompile.expression
{
	public class UpvalueExpression : Expression
	{
		private readonly string _name;

		public UpvalueExpression(string name) : base(PRECEDENCE_ATOMIC)
		{
			_name = name;
		}

		public override int GetConstantIndex() => -1;
		public override bool IsDotChain() => true;
		public override void Print(Decompiler d, Output output) => output.Print(_name);
		public override bool IsBrief() => true;
		public override bool IsIdentifier() => true;
		public override string AsName() => _name;
		public override bool IsEnvironmentTable(Decompiler d) => d.getVersion().isEnvironmentTable(_name);
	}
}
