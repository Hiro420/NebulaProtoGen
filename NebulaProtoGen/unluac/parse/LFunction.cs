namespace unluac.parse
{
	public class LFunction : BObject
	{
		public BHeader header; public LFunction? parent; public int[] code = System.Array.Empty<int>();
		public LLocal[] locals = System.Array.Empty<LLocal>();
		public LObject[] constants = System.Array.Empty<LObject>();
		public LUpvalue[] upvalues = System.Array.Empty<LUpvalue>();
		public LFunction[] functions = System.Array.Empty<LFunction>();
		public int maximumStackSize; public int numUpvalues; public int numParams; public int vararg; public bool stripped;
		public LFunction(BHeader header, int[] code, LLocal[] locals, LObject[] constants, LUpvalue[] upvalues, LFunction[] functions, int maximumStackSize, int numUpValues, int numParams, int vararg)
		{
			this.header = header; this.code = code; this.locals = locals; this.constants = constants; this.upvalues = upvalues; this.functions = functions; this.maximumStackSize = maximumStackSize; this.numUpvalues = numUpValues; this.numParams = numParams; this.vararg = vararg; stripped = false;
		}
	}
}
