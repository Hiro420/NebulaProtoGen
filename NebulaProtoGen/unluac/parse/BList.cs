namespace unluac.parse
{
	public class BList<T> : BObject where T : BObject
	{
		public readonly BInteger length;
		private readonly List<T> values;
		public BList(BInteger length, List<T> values) { this.length = length; this.values = values; }
		public T Get(int index) => values[index];
		public T[] AsArray(T[] array)
		{
			int i = 0; length.Iterate(() => { array[i] = values[i]; i++; }); return array;
		}
	}
}