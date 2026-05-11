namespace unluac.decompile.expression
{
	public class LocalVariable : Expression
	{
		private readonly Declaration _decl;

		public LocalVariable(Declaration decl) : base(PRECEDENCE_ATOMIC)
		{
			_decl = decl;
		}

		public override int GetConstantIndex() => -1;
		public override bool IsDotChain() => true;
		public override void Print(Decompiler d, Output output) => output.Print(_decl.name);
		public override bool IsBrief() => true;
		public override bool IsIdentifier() => true;
		public override string AsName() => _decl.name;
		// Expose declaration for binding scenarios
		public Declaration Declaration => _decl;
	}
}
