using unluac.decompile.statement;
using unluac.parse;

namespace unluac.decompile.block
{
	public class BooleanIndicator : Block
	{
		public BooleanIndicator(LFunction function, int line) : base(function, line, line)
		{
		}

		public override void AddStatement(Statement statement)
		{
			// No statements stored? 
		}

		public override bool IsContainer() => false;
		public override bool IsUnprotected() => false;
		public override bool Breakable() => false;
		public override int GetLoopback() { throw new System.InvalidOperationException(); }

		public override void Print(Decompiler d, Output output)
		{
			output.Print("-- unhandled boolean indicator");
		}
	}
}
