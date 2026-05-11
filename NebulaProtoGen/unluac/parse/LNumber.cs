namespace unluac.parse
{
	public abstract class LNumber : LObject
	{
		public static LNumber MakeInteger(int number) => new LIntNumber(number);
		public abstract override string ToString();
		public abstract double Value();
	}
	public class LFloatNumber : LNumber
	{
		public readonly float number; public readonly LNumberType.NumberMode mode;
		public LFloatNumber(float number, LNumberType.NumberMode mode) { this.number = number; this.mode = mode; }
		public override string ToString() => (mode == LNumberType.NumberMode.MODE_NUMBER && number == MathF.Round(number)) ? ((int)number).ToString() : number.ToString();
		public override bool Equals(object? o) => o is LFloatNumber f ? number == f.number : o is LNumber ln && Value() == ln.Value();
		public override double Value() => number;
	}
	public class LDoubleNumber : LNumber
	{
		public readonly double number; public readonly LNumberType.NumberMode mode;
		public LDoubleNumber(double number, LNumberType.NumberMode mode) { this.number = number; this.mode = mode; }
		public override string ToString() => (mode == LNumberType.NumberMode.MODE_NUMBER && number == Math.Round(number)) ? ((long)number).ToString() : number.ToString();
		public override bool Equals(object? o) => o is LDoubleNumber d ? number == d.number : o is LNumber ln && Value() == ln.Value();
		public override double Value() => number;
	}
	public class LIntNumber : LNumber
	{
		public readonly int number; public LIntNumber(int number) { this.number = number; }
		public override string ToString()
		{
			// Lua integer constants may be treated as unsigned when originating from 16-bit fields.
			// If negative but fits in 16-bit signed range, display unsigned equivalent.
			if (number < 0 && number >= -65535)
			{
				return (number & 0xFFFF).ToString();
			}
			return number.ToString();
		}
		public override bool Equals(object? o) => o is LIntNumber i ? number == i.number : o is LNumber ln && Value() == ln.Value();
		public override double Value() => number;
	}
	public class LLongNumber : LNumber
	{
		public readonly long number; public LLongNumber(long number) { this.number = number; }
		public override string ToString() => number.ToString();
		public override bool Equals(object? o) => o is LLongNumber l ? number == l.number : o is LNumber ln && Value() == ln.Value();
		public override double Value() => number;
	}
}
