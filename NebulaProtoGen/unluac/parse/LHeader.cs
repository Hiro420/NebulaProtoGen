using unluac.decompile;

namespace unluac.parse
{
	public class LHeader : BObject
	{
		public readonly int format;
		public readonly BIntegerType integer;
		public readonly BSizeTType sizeT;
		public readonly LBooleanType @bool;
		public readonly LNumberType number;
		public readonly LNumberType? linteger;
		public readonly LNumberType? lfloat;
		public readonly LStringType @string;
		public readonly LConstantType constant;
		public readonly LLocalType local;
		public readonly LUpvalueType upvalue;
		public readonly LFunctionType function;
		public readonly CodeExtract extractor;
		public LHeader(int format, BIntegerType integer, BSizeTType sizeT, LBooleanType boolType, LNumberType number, LNumberType? linteger, LNumberType? lfloat, LStringType stringType, LConstantType constant, LLocalType local, LUpvalueType upvalue, LFunctionType function, CodeExtract extractor)
		{
			this.format = format; this.integer = integer; this.sizeT = sizeT; this.@bool = boolType; this.number = number; this.linteger = linteger; this.lfloat = lfloat; this.@string = stringType; this.constant = constant; this.local = local; this.upvalue = upvalue; this.function = function; this.extractor = extractor;
		}
	}
}
