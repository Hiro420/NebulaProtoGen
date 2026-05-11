using unluac.decompile.block;
using unluac.decompile.expression;
using unluac.decompile.statement;

namespace unluac.decompile.operation
{
	public class CallOperation : Operation
	{
		private readonly FunctionCall _call;
		public CallOperation(int line, FunctionCall call) : base(line) { _call = call; }
		public override Statement process(Registers r, Block block) { return new FunctionCallStatement(_call); }
	}
}
