using unluac.decompile;
using unluac.util;

namespace unluac.parse
{
	public class BHeader
	{
		private static readonly byte[] signature = { 0x1B, 0x4C, 0x75, 0x61 };
		public readonly bool debug = false;
		public readonly Configuration config; public readonly Version version; public readonly LHeader lheader; public readonly BIntegerType integer; public readonly BSizeTType sizeT; public readonly LBooleanType @bool; public readonly LNumberType number; public readonly LNumberType? linteger; public readonly LNumberType? lfloat; public readonly LStringType @string; public readonly LConstantType constant; public readonly LLocalType local; public readonly LUpvalueType upvalue; public readonly LFunctionType function; public readonly CodeExtract extractor; public readonly LFunction main;
		public BHeader(ByteBuffer buffer, Configuration config)
		{
			this.config = config;
			for (int i = 0; i < signature.Length; i++) { if (buffer.Get() != signature[i]) throw new InvalidOperationException("Invalid Lua signature"); }
			int versionNumber = 0xFF & buffer.Get();
			version = versionNumber switch { 0x50 => Version.LUA50, 0x51 => Version.LUA51, 0x52 => Version.LUA52, 0x53 => Version.LUA53, _ => throw new InvalidOperationException("Unsupported Lua version " + ((versionNumber >> 4) + "." + (versionNumber & 0x0F))) };
			lheader = version.GetLHeaderType().Parse(buffer, this);
			integer = lheader.integer; sizeT = lheader.sizeT; @bool = lheader.@bool; number = lheader.number; linteger = lheader.linteger; lfloat = lheader.lfloat; @string = lheader.@string; constant = lheader.constant; local = lheader.local; upvalue = lheader.upvalue; function = lheader.function; extractor = lheader.extractor;
			int upvalues = -1; if (versionNumber >= 0x53) { upvalues = 0xFF & buffer.Get(); if (debug) System.Console.WriteLine("-- main chunk upvalue count: " + upvalues); }
			main = function.Parse(buffer, this);
			if (upvalues >= 0 && main.numUpvalues != upvalues) throw new InvalidOperationException("Main chunk wrong number of upvalues: " + main.numUpvalues + " (" + upvalues + " expected)");
			if (main.numUpvalues >= 1 && versionNumber >= 0x52 && (main.upvalues[0].name == null || main.upvalues[0].name.Length == 0)) main.upvalues[0].name = "_ENV";
		}
	}
}
