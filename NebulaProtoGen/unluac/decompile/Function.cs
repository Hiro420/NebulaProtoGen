using unluac.decompile.expression;
using unluac.parse;

namespace unluac.decompile
{
	// Function wrapper for constants/global resolution.
	public class Function
	{
		private readonly Constant[] _constants; private readonly int _constantsOffset;
		public Function(LFunction function) { _constants = new Constant[function.constants.Length]; for (int i = 0; i < _constants.Length; i++) _constants[i] = new Constant(function.constants[i]); _constantsOffset = function.header.version.versionNumber == 0x50 ? 250 : 256; }
		public Constant[] constants => _constants;
		public bool isConstant(int register) => register >= _constantsOffset;
		public int constantIndex(int register) => register - _constantsOffset;
		public string getGlobalName(int constantIndex) => _constants[constantIndex].AsName();
		public ConstantExpression getConstantExpression(int constantIndex) => new ConstantExpression(_constants[constantIndex], constantIndex);
		public GlobalExpression getGlobalExpression(int constantIndex) => new GlobalExpression(getGlobalName(constantIndex), constantIndex);
	}
}
