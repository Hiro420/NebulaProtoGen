using unluac.decompile.block;
using unluac.decompile.expression;
using unluac.decompile.statement;
using unluac.decompile.target;

namespace unluac.decompile.operation
{
	public class UpvalueSet : Operation
	{
		private readonly string _name; private readonly Expression _value;
		public UpvalueSet(int line, string name, Expression value) : base(line) { _name = name; _value = value; }
		public override Statement process(Registers r, Block block) { var assign = new Assignment(); assign.AddLast(new UpvalueTarget(_name), _value); return assign; }
	}
}
