using unluac.parse;

namespace unluac.decompile.expression
{
	public class ClosureExpression : Expression
	{
		private readonly LFunction _function; private readonly int _line;
		public ClosureExpression(LFunction f, Declaration[] parentDecls, int line) : base(PRECEDENCE_ATOMIC) { _function = f; _line = line; }
		public override void Print(Decompiler d, Output output)
		{
			// Simplified closure printing until nested decompilation wiring is complete.
			output.Print("function(...)");
			output.Print(" end");
		}
		public override int GetConstantIndex() => -1; public override bool IsClosure() => true; public override bool IsUngrouped() => false; public override bool IsMultiple() => false; public override bool IsIdentifier() => false; public override int ClosureUpvalueLine() => _line;
	}
}
