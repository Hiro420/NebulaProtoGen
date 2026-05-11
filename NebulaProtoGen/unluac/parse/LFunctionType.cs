using unluac.util;

namespace unluac.parse
{
	public class LFunctionType : BObjectType<LFunction>
	{
		public static readonly LFunctionType TYPE50 = new LFunctionType50();
		public static readonly LFunctionType TYPE51 = new LFunctionType();
		public static readonly LFunctionType TYPE52 = new LFunctionType52();
		public static readonly LFunctionType TYPE53 = new LFunctionType53();

		protected class State { public LString? name; public int lineBegin; public int lineEnd; public int lenUpvalues; public int lenParameter; public int vararg; public int maximumStackSize; public int length; public int[] code = System.Array.Empty<int>(); public BList<LObject>? constants; public BList<LFunction>? functions; public BList<BInteger>? lines; public BList<LLocal>? locals; public LUpvalue[] upvalues = System.Array.Empty<LUpvalue>(); }

		public override LFunction Parse(ByteBuffer buffer, BHeader header)
		{
			if (header.debug) System.Console.WriteLine("-- beginning to parse function");
			var s = new State(); ParseMain(buffer, header, s);
			var lf = new LFunction(header, s.code, s.locals!.AsArray(new LLocal[s.locals.length.AsInt()]), s.constants!.AsArray(new LObject[s.constants.length.AsInt()]), s.upvalues, s.functions!.AsArray(new LFunction[s.functions.length.AsInt()]), s.maximumStackSize, s.lenUpvalues, s.lenParameter, s.vararg);
			foreach (var child in lf.functions) child.parent = lf;
			if (s.lines!.length.AsInt() == 0 && s.locals.length.AsInt() == 0) lf.stripped = true;
			return lf;
		}
		protected virtual void ParseMain(ByteBuffer buffer, BHeader header, State s)
		{
			s.name = header.@string.Parse(buffer, header); s.lineBegin = header.integer.Parse(buffer, header).AsInt(); s.lineEnd = header.integer.Parse(buffer, header).AsInt(); s.lenUpvalues = 0xFF & buffer.Get(); s.lenParameter = 0xFF & buffer.Get(); s.vararg = 0xFF & buffer.Get(); s.maximumStackSize = 0xFF & buffer.Get(); ParseCode(buffer, header, s); ParseConstants(buffer, header, s); ParseUpvalues(buffer, header, s); ParseDebug(buffer, header, s);
		}
		protected void ParseCode(ByteBuffer buffer, BHeader header, State s) { s.length = header.integer.Parse(buffer, header).AsInt(); s.code = new int[s.length]; for (int i = 0; i < s.length; i++) { s.code[i] = buffer.GetInt(); if (header.debug) System.Console.WriteLine("-- parsed codepoint " + s.code[i].ToString("X")); } }
		protected void ParseConstants(ByteBuffer buffer, BHeader header, State s) { s.constants = header.constant.ParseList(buffer, header); s.functions = header.function.ParseList(buffer, header); }
		protected virtual void ParseUpvalues(ByteBuffer buffer, BHeader header, State s) { s.upvalues = new LUpvalue[s.lenUpvalues]; for (int i = 0; i < s.lenUpvalues; i++) s.upvalues[i] = new LUpvalue(); }
		protected virtual void ParseDebug(ByteBuffer buffer, BHeader header, State s) { s.lines = header.integer.ParseList(buffer, header); s.locals = header.local.ParseList(buffer, header); var names = header.@string.ParseList(buffer, header); for (int i = 0; i < names.length.AsInt(); i++) s.upvalues[i].name = names.Get(i).Deref(); }
	}
	public class LFunctionType52 : LFunctionType
	{
		protected override void ParseMain(ByteBuffer buffer, BHeader header, State s) { s.lineBegin = header.integer.Parse(buffer, header).AsInt(); s.lineEnd = header.integer.Parse(buffer, header).AsInt(); s.lenParameter = 0xFF & buffer.Get(); s.vararg = 0xFF & buffer.Get(); s.maximumStackSize = 0xFF & buffer.Get(); ParseCode(buffer, header, s); ParseConstants(buffer, header, s); ParseUpvalues(buffer, header, s); ParseDebug(buffer, header, s); }
		protected override void ParseDebug(ByteBuffer buffer, BHeader header, State s) { s.name = header.@string.Parse(buffer, header); base.ParseDebug(buffer, header, s); }
		protected override void ParseUpvalues(ByteBuffer buffer, BHeader header, State s) { var ups = header.upvalue.ParseList(buffer, header); s.lenUpvalues = ups.length.AsInt(); s.upvalues = ups.AsArray(new LUpvalue[s.lenUpvalues]); }
	}
	public class LFunctionType53 : LFunctionType
	{
		protected override void ParseMain(ByteBuffer buffer, BHeader header, State s) { s.name = header.@string.Parse(buffer, header); s.lineBegin = header.integer.Parse(buffer, header).AsInt(); s.lineEnd = header.integer.Parse(buffer, header).AsInt(); s.lenParameter = 0xFF & buffer.Get(); s.vararg = 0xFF & buffer.Get(); s.maximumStackSize = 0xFF & buffer.Get(); ParseCode(buffer, header, s); s.constants = header.constant.ParseList(buffer, header); ParseUpvalues(buffer, header, s); s.functions = header.function.ParseList(buffer, header); ParseDebug(buffer, header, s); }
		protected override void ParseUpvalues(ByteBuffer buffer, BHeader header, State s) { var ups = header.upvalue.ParseList(buffer, header); s.lenUpvalues = ups.length.AsInt(); s.upvalues = ups.AsArray(new LUpvalue[s.lenUpvalues]); }
	}
	public class LFunctionType50 : LFunctionType
	{
		protected override void ParseMain(ByteBuffer buffer, BHeader header, State s) { s.name = header.@string.Parse(buffer, header); s.lineBegin = header.integer.Parse(buffer, header).AsInt(); s.lineEnd = 0; s.lenUpvalues = 0xFF & buffer.Get(); s.upvalues = new LUpvalue[s.lenUpvalues]; for (int i = 0; i < s.lenUpvalues; i++) s.upvalues[i] = new LUpvalue(); s.lenParameter = 0xFF & buffer.Get(); s.vararg = 0xFF & buffer.Get(); s.maximumStackSize = 0xFF & buffer.Get(); ParseDebug(buffer, header, s); ParseConstants(buffer, header, s); ParseCode(buffer, header, s); }
		protected override void ParseUpvalues(ByteBuffer buffer, BHeader header, State s) { var ups = header.upvalue.ParseList(buffer, header); s.lenUpvalues = ups.length.AsInt(); s.upvalues = ups.AsArray(new LUpvalue[s.lenUpvalues]); }
	}
}
