using unluac.util;

namespace unluac.parse
{
	public class LNumberType : BObjectType<LNumber>
	{
		public enum NumberMode { MODE_NUMBER, MODE_FLOAT, MODE_INTEGER }
		public readonly int size; public readonly bool integral; public readonly NumberMode mode;
		public LNumberType(int size, bool integral, NumberMode mode) { this.size = size; this.integral = integral; this.mode = mode; if (!(size == 4 || size == 8)) throw new InvalidOperationException("Unsupported number size: " + size); }
		public double Convert(double number)
		{
			if (integral) return size == 4 ? (int)number : (long)number;
			return size == 4 ? (float)number : number;
		}
		public override LNumber Parse(ByteBuffer buffer, BHeader header)
		{
			LNumber value;
			if (integral)
			{
				value = size == 4 ? new LIntNumber(buffer.GetInt()) : new LLongNumber(buffer.GetLong());
			}
			else
			{
				value = size == 4 ? new LFloatNumber(buffer.GetFloat(), mode) : new LDoubleNumber(buffer.GetDouble(), mode);
			}
			if (header.debug) System.Console.WriteLine("-- parsed <number> " + value);
			return value;
		}
	}
}
