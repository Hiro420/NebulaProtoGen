using unluac.decompile.block;
using unluac.decompile.statement;

namespace unluac.decompile.operation
{
	public abstract class Operation
	{
		public readonly int line;
		protected Operation(int line) { this.line = line; }
		public abstract Statement process(Registers r, Block block);
	}
}
