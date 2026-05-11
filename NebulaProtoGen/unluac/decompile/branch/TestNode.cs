using unluac.decompile.expression;

namespace unluac.decompile.branch
{
	public class TestNode : Branch
	{
		public readonly int test; public readonly bool invert;
		public TestNode(int test, bool invert, int line, int begin, int end) : base(line, begin, end) { this.test = test; this.invert = invert; isTest = true; }
		public override Branch Invert() { return new TestNode(test, !invert, line, end, begin); }
		public override int GetRegister() { return test; }
		public override Expression AsExpression(Registers r) { if (invert) { return new NotBranch(this.Invert()).AsExpression(r); } else { return r.getExpression(test, line); } }
		public override void UseExpression(Expression expression) { }
		public override string ToString() { return $"TestNode[test={test};invert={invert};line={line};begin={begin};end={end}]"; }
	}
}
