using unluac.decompile.statement;
using unluac.parse;

namespace unluac.decompile.block
{
	public class Break : Block
	{
		public readonly int target;

		public Break(LFunction function, int line, int target) : base(function, line, line)
		{
			this.target = target;
		}

		public override void AddStatement(Statement statement)
		{
			throw new System.InvalidOperationException();
		}

		public override bool IsContainer() => false;
		public override bool Breakable() => false;
		public override bool IsUnprotected() => false;
		public override int GetLoopback() { throw new System.InvalidOperationException(); }

		public override void Print(Decompiler d, Output output)
		{
			output.Print("do break end");
		}

		public override void PrintTail(Decompiler d, Output output) { output.Print("break"); }
	}
}
