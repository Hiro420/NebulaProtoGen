using unluac.decompile.expression;

namespace unluac.decompile.branch
{
	public class LTNode : Branch
	{
		private readonly int left; private readonly int right; private readonly bool invert;
		public LTNode(int left, int right, bool invert, int line, int begin, int end) : base(line, begin, end) { this.left = left; this.right = right; this.invert = invert; }
		public override Branch Invert() { return new LTNode(left, right, !invert, line, end, begin); }
		public override int GetRegister() { return -1; }
		public override Expression AsExpression(Registers r) { bool transpose = false; Expression leftExpression = r.getKExpression(left, line); Expression rightExpression = r.getKExpression(right, line); if (!leftExpression.IsConstant() && !rightExpression.IsConstant()) { transpose = r.getUpdated(left, line) > r.getUpdated(right, line); } else { transpose = rightExpression.GetConstantIndex() < leftExpression.GetConstantIndex(); } string op = !transpose ? "<" : ">"; Expression rtn = new expression.BinaryExpression(op, !transpose ? leftExpression : rightExpression, !transpose ? rightExpression : leftExpression, Expression.PRECEDENCE_COMPARE, Expression.ASSOCIATIVITY_LEFT); if (invert) { rtn = new expression.UnaryExpression("not ", rtn, Expression.PRECEDENCE_UNARY); } return rtn; }
		public override void UseExpression(Expression expression) { }
	}
}
