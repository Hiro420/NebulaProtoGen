using unluac.decompile.expression;
using unluac.parse;

namespace unluac.decompile
{
	public class Upvalues
	{
		private readonly LUpvalue[] _upvalues;
		public Upvalues(LFunction func, Declaration[] parentDecls, int line) { _upvalues = func.upvalues; foreach (var upvalue in _upvalues) { if (upvalue.name == null || upvalue.name.Length == 0) { if (upvalue.instack) { if (parentDecls != null) { foreach (var decl in parentDecls) { if (decl.register == upvalue.idx && line >= decl.begin && line < decl.end) { upvalue.name = decl.name; break; } } } } else { var parentvals = func.parent.upvalues; if (upvalue.idx >= 0 && upvalue.idx < parentvals.Length) { upvalue.name = parentvals[upvalue.idx].name; } } } } }
		public string getName(int index) { if (index < _upvalues.Length && _upvalues[index].name != null && _upvalues[index].name.Length > 0) return _upvalues[index].name; return "_UPVALUE" + index + "_"; }
		public UpvalueExpression getExpression(int index) => new UpvalueExpression(getName(index));
	}
}
