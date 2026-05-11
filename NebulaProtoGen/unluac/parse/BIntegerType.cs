using System.Numerics;
using unluac.util;

namespace unluac.parse
{
	public class BIntegerType : BObjectType<BInteger>
	{
		public readonly int intSize;
		public BIntegerType(int intSize) { this.intSize = intSize; }

		internal BInteger RawParse(ByteBuffer buffer)
		{
			BInteger value;
			switch (intSize)
			{
				case 0: value = new BInteger(0); break;
				case 1: value = new BInteger((sbyte)buffer.Get()); break;
				case 2: value = new BInteger(buffer.GetShort()); break;
				case 4: value = new BInteger(buffer.GetInt()); break;
				default:
					byte[] bytes = new byte[intSize];
					// Reverse if little-endian
					if (buffer.Order == ByteOrder.LittleEndian)
					{
						for (int i = intSize - 1; i >= 0; i--) bytes[i] = buffer.Get();
					}
					else
					{
						for (int i = 0; i < intSize; i++) bytes[i] = buffer.Get();
					}
					value = new BInteger(new BigInteger(bytes));
					break;
			}
			return value;
		}

		public override BInteger Parse(ByteBuffer buffer, BHeader header)
		{
			var value = RawParse(buffer);
			if (header.debug) Console.WriteLine("-- parsed <integer> " + value.AsInt());
			return value;
		}
	}
}