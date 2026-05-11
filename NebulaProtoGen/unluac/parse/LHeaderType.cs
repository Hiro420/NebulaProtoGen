using unluac.decompile;
using unluac.util;

namespace unluac.parse
{
	public abstract class LHeaderType : BObjectType<LHeader>
	{
		public static readonly LHeaderType TYPE50 = new LHeaderType50();
		public static readonly LHeaderType TYPE51 = new LHeaderType51();
		public static readonly LHeaderType TYPE52 = new LHeaderType52();
		public static readonly LHeaderType TYPE53 = new LHeaderType53();
		private static readonly byte[] luacTail = { 0x19, 0x93, 0x0D, 0x0A, 0x1A, 0x0A };
		protected class S { public BIntegerType? integer; public BSizeTType? sizeT; public LNumberType? number; public LNumberType? linteger; public LNumberType? lfloat; public LStringType? stringType; public LConstantType? constant; public LFunctionType? function; public CodeExtract? extractor; public int format; public int lNumberSize; public bool lNumberIntegrality; public int lIntegerSize; public int lFloatSize; }
		public override LHeader Parse(ByteBuffer buffer, BHeader header) { var s = new S(); ParseMain(buffer, header, s); var boolType = new LBooleanType(); var local = new LLocalType(); var upvalue = new LUpvalueType(); return new LHeader(s.format, s.integer!, s.sizeT!, boolType, s.number!, s.linteger, s.lfloat, s.stringType!, s.constant!, local, upvalue, s.function!, s.extractor!); }
		protected abstract void ParseMain(ByteBuffer buffer, BHeader header, S s);
		protected void ParseFormat(ByteBuffer buffer, BHeader header, S s) { int format = 0xFF & buffer.Get(); if (format != 0) throw new InvalidOperationException("Non-standard lua format: " + format); s.format = format; if (header.debug) System.Console.WriteLine("-- format: " + format); }
		protected void ParseEndianness(ByteBuffer buffer, BHeader header, S s) { int e = 0xFF & buffer.Get(); switch (e) { case 0: buffer.OrderSet(ByteOrder.BigEndian); break; case 1: buffer.OrderSet(ByteOrder.LittleEndian); break; default: throw new InvalidOperationException("Invalid endianness: " + e); } if (header.debug) System.Console.WriteLine("-- endianness: " + e + (e == 0 ? " (big)" : " (little)")); }
		protected void ParseIntSize(ByteBuffer buffer, BHeader header, S s) { int intSize = 0xFF & buffer.Get(); if (header.debug) System.Console.WriteLine("-- int size: " + intSize); s.integer = new BIntegerType(intSize); }
		protected void ParseSizeTSize(ByteBuffer buffer, BHeader header, S s) { int sizeTSize = 0xFF & buffer.Get(); if (header.debug) System.Console.WriteLine("-- size_t size: " + sizeTSize); s.sizeT = new BSizeTType(sizeTSize); }
		protected void ParseInstructionSize(ByteBuffer buffer, BHeader header, S s) { int instructionSize = 0xFF & buffer.Get(); if (header.debug) System.Console.WriteLine("-- instruction size: " + instructionSize); if (instructionSize != 4) throw new InvalidOperationException("Unsupported instruction size: " + instructionSize); }
		protected void ParseNumberSize(ByteBuffer buffer, BHeader header, S s) { int lns = 0xFF & buffer.Get(); if (header.debug) System.Console.WriteLine("-- Lua number size: " + lns); s.lNumberSize = lns; }
		protected void ParseNumberIntegrality(ByteBuffer buffer, BHeader header, S s) { int code = 0xFF & buffer.Get(); if (header.debug) System.Console.WriteLine("-- Lua number integrality code: " + code); if (code > 1) throw new InvalidOperationException("Invalid integrality code: " + code); s.lNumberIntegrality = (code == 1); }
		protected void ParseExtractor(ByteBuffer buffer, BHeader header, S s) { int sizeOp = 0xFF & buffer.Get(); int sizeA = 0xFF & buffer.Get(); int sizeB = 0xFF & buffer.Get(); int sizeC = 0xFF & buffer.Get(); if (header.debug) System.Console.WriteLine("-- Lua opcode extractor sizeOp: " + sizeOp + ", sizeA: " + sizeA + ", sizeB: " + sizeB + ", sizeC: " + sizeC); s.extractor = new Code50(sizeOp, sizeA, sizeB, sizeC); }
		protected void ParseTail(ByteBuffer buffer, BHeader header, S s) { for (int i = 0; i < luacTail.Length; i++) { if (buffer.Get() != luacTail[i]) throw new InvalidOperationException("Invalid header tail"); } }
	}
	public class LHeaderType50 : LHeaderType
	{
		private const double TEST_NUMBER = 3.14159265358979323846E7;
		protected override void ParseMain(ByteBuffer buffer, BHeader header, S s) { s.format = 0; ParseEndianness(buffer, header, s); ParseIntSize(buffer, header, s); ParseSizeTSize(buffer, header, s); ParseInstructionSize(buffer, header, s); ParseExtractor(buffer, header, s); ParseNumberSize(buffer, header, s); var lfloat = new LNumberType(s.lNumberSize, false, LNumberType.NumberMode.MODE_NUMBER); var linteger = new LNumberType(s.lNumberSize, true, LNumberType.NumberMode.MODE_NUMBER); buffer.Mark(); double floatcheck = lfloat.Parse(buffer, header).Value(); buffer.Reset(); double intcheck = linteger.Parse(buffer, header).Value(); if (floatcheck == lfloat.Convert(TEST_NUMBER)) s.number = lfloat; else if (intcheck == linteger.Convert(TEST_NUMBER)) s.number = linteger; else throw new InvalidOperationException("Unrecognized number format: " + intcheck); s.function = LFunctionType.TYPE50; s.stringType = LStringType.GetType50(); s.constant = LConstantType.GetType50(); }
	}
	public class LHeaderType51 : LHeaderType
	{
		protected override void ParseMain(ByteBuffer buffer, BHeader header, S s) { ParseFormat(buffer, header, s); ParseEndianness(buffer, header, s); ParseIntSize(buffer, header, s); ParseSizeTSize(buffer, header, s); ParseInstructionSize(buffer, header, s); ParseNumberSize(buffer, header, s); ParseNumberIntegrality(buffer, header, s); s.number = new LNumberType(s.lNumberSize, s.lNumberIntegrality, LNumberType.NumberMode.MODE_NUMBER); s.function = LFunctionType.TYPE51; s.stringType = LStringType.GetType50(); s.constant = LConstantType.GetType50(); s.extractor = Code.Code51; }
	}
	public class LHeaderType52 : LHeaderType
	{
		protected override void ParseMain(ByteBuffer buffer, BHeader header, S s) { ParseFormat(buffer, header, s); ParseEndianness(buffer, header, s); ParseIntSize(buffer, header, s); ParseSizeTSize(buffer, header, s); ParseInstructionSize(buffer, header, s); ParseNumberSize(buffer, header, s); ParseNumberIntegrality(buffer, header, s); ParseTail(buffer, header, s); s.number = new LNumberType(s.lNumberSize, s.lNumberIntegrality, LNumberType.NumberMode.MODE_NUMBER); s.function = LFunctionType.TYPE52; s.stringType = LStringType.GetType50(); s.constant = LConstantType.GetType50(); s.extractor = Code.Code51; }
	}
	public class LHeaderType53 : LHeaderType
	{
		protected override void ParseMain(ByteBuffer buffer, BHeader header, S s) { ParseFormat(buffer, header, s); ParseTail(buffer, header, s); ParseIntSize(buffer, header, s); ParseSizeTSize(buffer, header, s); ParseInstructionSize(buffer, header, s); int lIntegerSize = 0xFF & buffer.Get(); if (header.debug) System.Console.WriteLine("-- Lua integer size: " + lIntegerSize); if (lIntegerSize < 2) throw new InvalidOperationException("Integer size too small: " + lIntegerSize); s.lIntegerSize = lIntegerSize; int lFloatSize = 0xFF & buffer.Get(); if (header.debug) System.Console.WriteLine("-- Lua float size: " + lFloatSize); s.lFloatSize = lFloatSize; byte[] end = new byte[s.lIntegerSize]; buffer.Get(end); if (end[0] == 0x78 && end[1] == 0x56) buffer.OrderSet(ByteOrder.LittleEndian); else if (end[s.lIntegerSize - 1] == 0x78 && end[s.lIntegerSize - 2] == 0x56) buffer.OrderSet(ByteOrder.BigEndian); else throw new InvalidOperationException("Invalid endianness sequence: " + string.Join(",", end)); s.linteger = new LNumberType(s.lIntegerSize, true, LNumberType.NumberMode.MODE_INTEGER); s.lfloat = new LNumberType(s.lFloatSize, false, LNumberType.NumberMode.MODE_FLOAT); s.function = LFunctionType.TYPE53; s.stringType = LStringType.GetType53(); s.constant = LConstantType.GetType53(); s.extractor = Code.Code51; double floatcheck = s.lfloat.Parse(buffer, header).Value(); if (floatcheck != s.lfloat.Convert(370.5)) throw new InvalidOperationException("Unrecognized floating point format: " + floatcheck); }
	}
}
