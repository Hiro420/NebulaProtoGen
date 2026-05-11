using unluac.decompile.block;
using unluac.decompile.expression;
using unluac.decompile.statement;
using unluac.decompile.target;

namespace unluac.decompile.operation
{
	public class GlobalSet : Operation
	{
		private readonly string _name; private readonly Expression _value;
		public GlobalSet(int line, string name, Expression value) : base(line) { _name = name; _value = value; }
		public override Statement process(Registers r, Block block)
		{
			// Bind global name to underlying table literal (direct or via local register)
			if (_value is TableLiteral tlDirect && tlDirect.AssignedName == null)
			{
				tlDirect.BindName(_name);
			}
			else if (_value is LocalVariable lv)
			{
				var expr = r.GetValue(lv.Declaration.register, line); // value stored in register prior to global set
				if (expr is TableLiteral tl && tl.AssignedName == null)
				{
					tl.BindName(_name);
				}
			}
			var assign = new Assignment();
			assign.AddLast(new GlobalTarget(_name), _value);
			return assign;
		}
	}
}
