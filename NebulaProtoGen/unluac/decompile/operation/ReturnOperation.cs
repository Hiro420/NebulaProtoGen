using unluac.decompile.block;
using unluac.decompile.expression;
using unluac.decompile.statement;

namespace unluac.decompile.operation
{
	public class ReturnOperation : Operation
	{
		private readonly IList<Expression> _values;
		public ReturnOperation(int line, params Expression[] values) : base(line) { _values = values; }
		public ReturnOperation(int line, Expression value) : base(line) { _values = new List<Expression> { value }; }
		public override Statement process(Registers r, Block block) { return new Return(_values); }
	}
}
