using unluac.decompile.expression;

namespace unluac.decompile.target
{
	public class GlobalTarget : Target
	{
		private readonly string _name; private readonly int _index;
		public GlobalTarget(string name, int index = -1) { _name = name; _index = index; }
		public override void Print(Decompiler d, Output output) { output.Print(_name); }
		public override bool IsFunctionName() => true;
		public override Expression ToExpression() => new GlobalExpression(_name, _index);
	}
}
