using unluac.decompile.expression;

namespace unluac.decompile.branch
{
	public class NotBranch : Branch
	{
		private readonly Branch branch;
		public NotBranch(Branch branch) : base(branch.line, branch.begin, branch.end) { this.branch = branch; }
		public override Branch Invert() { return branch; }
		public override int GetRegister() { return branch.GetRegister(); }
		public override Expression AsExpression(Registers r) { return new expression.UnaryExpression("not ", branch.AsExpression(r), Expression.PRECEDENCE_UNARY); }
		public override void UseExpression(Expression expression) { }
	}
}
