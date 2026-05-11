using unluac.util;

namespace unluac.parse
{
	public class BSizeTType : BObjectType<BSizeT>
	{
		public readonly int sizeTSize; private readonly BIntegerType integerType;
		public BSizeTType(int sizeTSize) { this.sizeTSize = sizeTSize; integerType = new BIntegerType(sizeTSize); }
		public override BSizeT Parse(ByteBuffer buffer, BHeader header)
		{
			var value = new BSizeT(integerType.RawParse(buffer));
			if (header.debug) Console.WriteLine("-- parsed <size_t> " + value.AsInt());
			return value;
		}
	}
}