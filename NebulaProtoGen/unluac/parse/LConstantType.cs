using unluac.util;

namespace unluac.parse
{
	public abstract class LConstantType : BObjectType<LObject>
	{
		public static LConstantType50 GetType50() => new LConstantType50();
		public static LConstantType53 GetType53() => new LConstantType53();
	}
	public class LConstantType50 : LConstantType
	{
		public override LObject Parse(ByteBuffer buffer, BHeader header)
		{
			int type = 0xFF & buffer.Get();
			if (header.debug)
			{
				System.Console.Write("-- parsing <constant>, type is ");
				switch (type) { case 0: System.Console.WriteLine("<nil>"); break; case 1: System.Console.WriteLine("<boolean>"); break; case 3: System.Console.WriteLine("<number>"); break; case 4: System.Console.WriteLine("<string>"); break; default: System.Console.WriteLine("illegal " + type); break; }
			}
			return type switch { 0 => LNil.NIL, 1 => header.@bool.Parse(buffer, header), 3 => header.number.Parse(buffer, header), 4 => header.@string.Parse(buffer, header), _ => throw new InvalidOperationException() };
		}
	}
	public class LConstantType53 : LConstantType
	{
		public override LObject Parse(ByteBuffer buffer, BHeader header)
		{
			int type = 0xFF & buffer.Get();
			if (header.debug)
			{
				System.Console.Write("-- parsing <constant>, type is ");
				switch (type) { case 0: System.Console.WriteLine("<nil>"); break; case 1: System.Console.WriteLine("<boolean>"); break; case 3: System.Console.WriteLine("<float>"); break; case 0x13: System.Console.WriteLine("<integer>"); break; case 4: System.Console.WriteLine("<short string>"); break; case 0x14: System.Console.WriteLine("<long string>"); break; default: System.Console.WriteLine("illegal " + type); break; }
			}
			return type switch { 0 => LNil.NIL, 1 => header.@bool.Parse(buffer, header), 3 => header.lfloat.Parse(buffer, header), 0x13 => header.linteger.Parse(buffer, header), 4 => header.@string.Parse(buffer, header), 0x14 => header.@string.Parse(buffer, header), _ => throw new InvalidOperationException() };
		}
	}
}
