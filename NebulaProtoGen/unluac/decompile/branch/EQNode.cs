using unluac.decompile.expression;

namespace unluac.decompile.branch
{
	public class EQNode : Branch
	{
		private readonly int left; private readonly int right; private readonly bool invert;
		public EQNode(int left, int right, bool invert, int line, int begin, int end) : base(line, begin, end) { this.left = left; this.right = right; this.invert = invert; }
		public override Branch Invert() { return new EQNode(left, right, !invert, line, end, begin); }
		public override int GetRegister() { return -1; }
		public override Expression AsExpression(Registers r) { bool transpose = false; string op = invert ? "~=" : "=="; return new expression.BinaryExpression(op, r.getKExpression(!transpose ? left : right, line), r.getKExpression(!transpose ? right : left, line), Expression.PRECEDENCE_COMPARE, Expression.ASSOCIATIVITY_LEFT); }
		public override void UseExpression(Expression expression) { }
	}
}
