using unluac.parse;

namespace unluac.decompile
{
	public class Code
	{
		public static CodeExtract Code51 = new CodeExtract51();
		private readonly CodeExtract extractor; private readonly OpcodeMap map; private readonly int[] code;
		public Code(LFunction function) { code = function.code; map = function.header.version.GetOpcodeMap(); extractor = function.header.extractor; }
		public Op op(int line) => map.Get(opcode(line)) ?? throw new System.InvalidOperationException("Invalid opcode number: " + opcode(line));
		public int opcode(int line) => code[line - 1] & 0x3F;
		public int A(int line) => extractor.extract_A(code[line - 1]);
		public int B(int line) => extractor.extract_B(code[line - 1]);
		public int C(int line) => extractor.extract_C(code[line - 1]);
		public int Bx(int line) => extractor.extract_Bx(code[line - 1]);
		public int sBx(int line) => extractor.extract_sBx(code[line - 1]);
		public int codepoint(int line) => code[line - 1];
		public int length() => code.Length;
	}
	public class CodeExtract51 : CodeExtract
	{
		public int extract_A(int cp) => (cp >> 6) & 0xFF;
		public int extract_C(int cp) => (cp >> 14) & 0x1FF;
		public int extract_B(int cp) => (int)((uint)cp >> 23);
		public int extract_Bx(int cp) => (int)((uint)cp >> 14);
		public int extract_sBx(int cp) => (int)((uint)cp >> 14) - 131071;
		public int extract_op(int cp) => cp & 0x3F;
	}
	public interface CodeExtract
	{
		int extract_A(int codepoint); int extract_C(int codepoint); int extract_B(int codepoint); int extract_Bx(int codepoint); int extract_sBx(int codepoint); int extract_op(int codepoint);
	}
	public class Code50 : CodeExtract
	{
		private readonly int shiftA; private readonly int shiftB; private readonly int shiftBx; private readonly int shiftC; private readonly int maskOp; private readonly int maskA; private readonly int maskB; private readonly int maskBx; private readonly int maskC; private readonly int excessK;
		public Code50(int sizeOp, int sizeA, int sizeB, int sizeC) { shiftC = sizeOp; shiftB = sizeC + sizeOp; shiftBx = sizeOp; shiftA = sizeB + sizeC + sizeOp; maskOp = (1 << sizeOp) - 1; maskA = (1 << sizeA) - 1; maskB = (1 << sizeB) - 1; maskBx = (1 << (sizeB + sizeC)) - 1; maskC = (1 << sizeC) - 1; excessK = maskBx / 2; }
		public int extract_A(int cp) => (cp >> shiftA) & maskA; public int extract_C(int cp) => (cp >> shiftC) & maskC; public int extract_B(int cp) => (cp >> shiftB) & maskB; public int extract_Bx(int cp) => (cp >> shiftBx) & maskBx; public int extract_sBx(int cp) => ((cp >> shiftBx) & maskBx) - excessK; public int extract_op(int cp) => cp & maskOp;
	}
}
