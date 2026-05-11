using System.Text;
using unluac.util;

namespace unluac.parse
{
	public abstract class LStringType : BObjectType<LString>
	{
		public static LStringType50 GetType50() => new LStringType50();
		public static LStringType53 GetType53() => new LStringType53();
		protected readonly ThreadLocal<StringBuilder> b = new(() => new StringBuilder());
	}
	public class LStringType50 : LStringType
	{
		public override LString Parse(ByteBuffer buffer, BHeader header)
		{
			var sizeT = header.sizeT.Parse(buffer, header);
			var sb = b.Value!; sb.Clear();
			sizeT.Iterate(() => sb.Append((char)(0xFF & buffer.Get())));
			var s = sb.ToString(); if (header.debug) System.Console.WriteLine("-- parsed <string> \"" + s + "\"");
			return new LString(sizeT, s);
		}
	}
	public class LStringType53 : LStringType
	{
		public override LString Parse(ByteBuffer buffer, BHeader header)
		{
			BSizeT sizeT; int size = 0xFF & buffer.Get();
			if (size == 0xFF) sizeT = header.sizeT.Parse(buffer, header); else sizeT = new BSizeT(size);
			var sb = b.Value!; sb.Clear(); bool first = true;
			sizeT.Iterate(() => { if (first) first = false; else sb.Append((char)(0xFF & buffer.Get())); });
			sb.Append('\0'); var s = sb.ToString(); if (header.debug) System.Console.WriteLine("-- parsed <string> \"" + s + "\"");
			return new LString(sizeT, s);
		}
	}
}
