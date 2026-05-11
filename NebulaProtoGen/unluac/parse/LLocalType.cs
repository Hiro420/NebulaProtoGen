using unluac.util;

namespace unluac.parse
{
	public class LLocalType : BObjectType<LLocal>
	{
		public override LLocal Parse(ByteBuffer buffer, BHeader header)
		{
			var name = header.@string.Parse(buffer, header);
			var start = header.integer.Parse(buffer, header);
			var end = header.integer.Parse(buffer, header);
			if (header.debug)
			{
				System.Console.Write("-- parsing local, name: ");
				System.Console.Write(name);
				System.Console.Write(" from " + start.AsInt() + " to " + end.AsInt());
				System.Console.WriteLine();
			}
			return new LLocal(name, start, end);
		}
	}
}
