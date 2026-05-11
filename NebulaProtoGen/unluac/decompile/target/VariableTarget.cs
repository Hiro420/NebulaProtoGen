using unluac.decompile.expression;

namespace unluac.decompile.target
{
	public class VariableTarget : Target
	{
		private readonly Declaration _decl; public VariableTarget(Declaration decl) { _decl = decl; }
		public override void Print(Decompiler d, Output output) { output.Print(_decl.name); }
		public override bool IsDeclaration() => true;
		public override bool IsFunctionName() => true; // may be part of function name chain
		public override Expression ToExpression() => new LocalVariable(_decl);
		public override bool isDeclaration(Declaration decl) => _decl == decl;
	}
}
