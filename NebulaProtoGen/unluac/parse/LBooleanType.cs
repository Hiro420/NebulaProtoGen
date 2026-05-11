using unluac.util;

namespace unluac.parse
{
	public class LBooleanType : BObjectType<LBoolean>
	{
		public override LBoolean Parse(ByteBuffer buffer, BHeader header)
		{
			int value = buffer.Get();
			if ((value & 0xFFFFFFFE) != 0) throw new InvalidOperationException();
			var b = value == 0 ? LBoolean.LFALSE : LBoolean.LTRUE;
			if (header.debug) System.Console.WriteLine("-- parsed <boolean> " + b);
			return b;
		}
	}
}
