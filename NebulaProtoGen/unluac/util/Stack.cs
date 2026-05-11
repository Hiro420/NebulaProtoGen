namespace unluac.util
{
	// Minimal stack with reverse capability used by decompiler logic
	public class Stack<T> : IEnumerable<T>
	{
		private readonly List<T> _list = new();
		public void push(T item) => _list.Add(item);
		public T pop() { var idx = _list.Count - 1; var v = _list[idx]; _list.RemoveAt(idx); return v; }
		public T peek() => _list[_list.Count - 1];
		public T Peek() => peek();
		public bool isEmpty() => _list.Count == 0;
		public void reverse() { _list.Reverse(); }
		public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
