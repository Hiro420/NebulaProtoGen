using unluac.decompile.expression;

namespace unluac.decompile.branch
{
	public class AndBranch : Branch
	{
		private readonly Branch left; private readonly Branch right;
		public AndBranch(Branch left, Branch right) : base(right.line, right.begin, right.end) { this.left = left; this.right = right; }
		public override Branch Invert() { return new OrBranch(left.Invert(), right.Invert()); }
		public override int GetRegister() { int rleft = left.GetRegister(); int rright = right.GetRegister(); return rleft == rright ? rleft : -1; }
		public override Expression AsExpression(Registers r) { return new expression.BinaryExpression("and", left.AsExpression(r), right.AsExpression(r), Expression.PRECEDENCE_AND, Expression.ASSOCIATIVITY_NONE); }
		public override void UseExpression(Expression expression) { left.UseExpression(expression); right.UseExpression(expression); }
	}
}
