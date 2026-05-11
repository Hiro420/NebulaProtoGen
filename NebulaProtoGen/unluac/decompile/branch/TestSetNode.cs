using unluac.decompile.expression;

namespace unluac.decompile.branch
{
	public class TestSetNode : Branch
	{
		public readonly int test; public readonly bool invert;
		public TestSetNode(int target, int test, bool invert, int line, int begin, int end) : base(line, begin, end) { this.test = test; this.invert = invert; setTarget = target; }
		public override Branch Invert() { return new TestSetNode(setTarget, test, !invert, line, end, begin); }
		public override int GetRegister() { return setTarget; }
		public override Expression AsExpression(Registers r) { return r.getExpression(test, line); }
		public override void UseExpression(Expression expression) { }
		public override string ToString() { return $"TestSetNode[target={setTarget};test={test};invert={invert};line={line};begin={begin};end={end}]"; }
	}
}
