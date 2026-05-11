using unluac.decompile.block;
using unluac.decompile.expression;
using unluac.decompile.statement;

namespace unluac.decompile.operation
{
	public class RegisterSet : Operation
	{
		public readonly int register; public readonly Expression value;
		public RegisterSet(int line, int register, Expression value) : base(line) { this.register = register; this.value = value; }
		public override Statement process(Registers r, Block block)
		{
			// Bind name to TableLiteral (extra aid compared to Java) before storing value
			if (value is TableLiteral tl)
			{
				var declCurrent = r.IsLocal(register, line) ? r.GetDeclaration(register, line) : null;
				var declPrev = (line > 1 && r.IsLocal(register, line - 1)) ? r.GetDeclaration(register, line - 1) : null;
				var decl = declCurrent ?? declPrev;
				if (decl != null) tl.BindName(decl.name);
			}
			// Store the value in register state
			r.setValue(register, line, value);
			// If register maps to a local that is assignable, emit an Assignment
			if (r.isAssignable(register, line))
			{
				return new Assignment(r.getTarget(register, line), value);
			}
			return null;
		}
	}
}
