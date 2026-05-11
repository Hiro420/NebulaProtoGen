using unluac.util;

namespace unluac.parse
{
	public class LSourceLines
	{
		public static LSourceLines? Parse(ByteBuffer buffer)
		{
			int number = buffer.GetInt(); while (number-- > 0) buffer.GetInt(); return null;
		}
	}
}
