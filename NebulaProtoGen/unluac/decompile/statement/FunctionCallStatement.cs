using unluac.decompile.expression;

namespace unluac.decompile.statement
{
	public class FunctionCallStatement : Statement
	{
		private readonly FunctionCall _call; public FunctionCallStatement(FunctionCall call) { _call = call; }
		public override void Print(Decompiler d, Output output) { _call.Print(d, output); output.Println(); }
	}
}
