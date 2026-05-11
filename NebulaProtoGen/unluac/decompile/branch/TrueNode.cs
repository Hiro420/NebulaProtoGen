using unluac.decompile.expression;
using unluac.parse;

namespace unluac.decompile.branch
{
	public class TrueNode : Branch
	{
		public readonly int register; public readonly bool invert;
		public TrueNode(int register, bool invert, int line, int begin, int end) : base(line, begin, end) { this.register = register; this.invert = invert; setTarget = register; }
		public override Branch Invert() { return new TrueNode(register, !invert, line, end, begin); }
		public override int GetRegister() { return register; }
		public override Expression AsExpression(Registers r) { return new expression.ConstantExpression(new Constant(invert ? LBoolean.LTRUE : LBoolean.LFALSE), -1); }
		public override void UseExpression(Expression expression) { }
		public override string ToString() { return $"TrueNode[invert={invert};line={line};begin={begin};end={end}]"; }
	}
}
