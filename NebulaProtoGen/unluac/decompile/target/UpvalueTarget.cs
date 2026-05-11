using unluac.decompile.expression;

namespace unluac.decompile.target
{
	public class UpvalueTarget : Target
	{
		private readonly string _name; public UpvalueTarget(string name) { _name = name; }
		public override void Print(Decompiler d, Output output) { output.Print(_name); }
		public override Expression ToExpression() => new UpvalueExpression(_name);
	}
}
