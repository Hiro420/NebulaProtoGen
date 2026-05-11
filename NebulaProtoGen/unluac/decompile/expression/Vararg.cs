namespace unluac.decompile.expression
{
	public class Vararg : Expression
	{
		private readonly int _count;
		private readonly bool _multiple;

		public Vararg(int count, bool multiple) : base(PRECEDENCE_ATOMIC)
		{
			_count = count;
			_multiple = multiple;
		}

		public override void Print(Decompiler d, Output output)
		{
			output.Print("...");
		}

		public override void PrintMultiple(Decompiler d, Output output)
		{
			Print(d, output);
		}

		public override int GetConstantIndex() => -1;
		public override bool IsMultiple() => _multiple;
		public override bool IsUngrouped() => true;
		public override bool IsBrief() => true;
	}
}
