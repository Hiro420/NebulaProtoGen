using unluac.util;

namespace unluac.parse
{
	public abstract class BObjectType<T> where T : BObject
	{
		public abstract T Parse(ByteBuffer buffer, BHeader header);
		public BList<T> ParseList(ByteBuffer buffer, BHeader header)
		{
			var length = header.integer.Parse(buffer, header);
			var values = new List<T>();
			length.Iterate(() => values.Add(Parse(buffer, header)));
			return new BList<T>(length, values);
		}
	}
}