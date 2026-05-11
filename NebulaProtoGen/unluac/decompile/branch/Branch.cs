using unluac.decompile.expression;

namespace unluac.decompile.branch
{
	public abstract class Branch
	{
		public readonly int line; public int begin; public int end; public bool isSet = false; public bool isCompareSet = false; public bool isTest = false; public int setTarget = -1;
		protected Branch(int line, int begin, int end) { this.line = line; this.begin = begin; this.end = end; }
		public abstract Branch Invert();
		public abstract int GetRegister();
		public abstract Expression AsExpression(Registers r);
		public abstract void UseExpression(Expression expression);

		public Branch invert() => Invert();
		public int getRegister() => GetRegister();
		public Expression asExpression(Registers r) => AsExpression(r);
		public void useExpression(Expression e) => UseExpression(e);
	}
}
