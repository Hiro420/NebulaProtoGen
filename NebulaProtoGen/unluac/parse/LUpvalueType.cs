using unluac.util;

namespace unluac.parse
{
	public class LUpvalueType : BObjectType<LUpvalue>
	{
		public override LUpvalue Parse(ByteBuffer buffer, BHeader header)
		{
			var up = new LUpvalue { instack = buffer.Get() != 0, idx = 0xFF & buffer.Get() };
			return up;
		}
	}
}
