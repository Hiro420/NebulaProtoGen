using unluac.decompile.expression;

namespace unluac.decompile.target
{
	public abstract class Target
	{
		public abstract void Print(Decompiler d, Output output);
		public virtual bool IsDeclaration() => false;
		public virtual bool IsFunctionName() => false;
		public virtual bool IsTableTarget() => false;
		public virtual Expression ToExpression() { throw new System.InvalidOperationException(); }

		// Port compatibility
		public virtual bool isDeclaration(Declaration decl) => false;
	}
}
