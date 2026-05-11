namespace unluac.parse
{
	public class LString : LObject
	{
		public readonly BSizeT size; public readonly string value;
		public LString(BSizeT size, string value) { this.size = size; this.value = value.Length == 0 ? "" : value.Substring(0, value.Length - 1); }
		public override string Deref() => value;
		public override string ToString() => '"' + value + '"';
		public override bool Equals(object? o) => o is LString ls && ls.value == value;
	}
}
