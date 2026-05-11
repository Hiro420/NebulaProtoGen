using unluac.decompile.expression;

namespace unluac.decompile.branch
{
	public class AssignNode : Branch
	{
		private Expression _expression;
		public AssignNode(int line, int begin, int end) : base(line, begin, end) { }
		public override Branch Invert() { throw new System.InvalidOperationException(); }
		public override int GetRegister() { throw new System.InvalidOperationException(); }
		public override Expression AsExpression(Registers r) { return _expression; }
		public override void UseExpression(Expression expression) { _expression = expression; }
	}
}
