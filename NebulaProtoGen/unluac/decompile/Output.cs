namespace unluac.decompile;

public class Output
{
	private readonly OutputProvider _provider; private int _indent = 0; private int _position = 0;
	public Output() : this(new ConsoleOutputProvider()) { }
	public Output(OutputProvider provider) { _provider = provider; }
	public void indent() { Indent(); }
	public void dedent() { Dedent(); }
	public void Indent() { _indent += 2; }
	public void Dedent() { _indent -= 2; }
	public int GetIndentationLevel() => _indent;
	public int GetPosition() => _position;
	public void SetIndentationLevel(int level) { _indent = level; }
	private void Start() { if (_position == 0) { for (int i = _indent; i > 0; i--) { _provider.Print(" "); _position++; } } }
	public void print(string s) { Start(); _provider.Print(s); _position += s.Length; }
	public void print(byte b) { Start(); _provider.Print(b); _position += 1; }
	public void println() { Start(); _provider.Println(); _position = 0; }
	public void println(string s) { print(s); println(); }
	public void Print(string s) => print(s);
	public void Print(byte b) => print(b);
	public void Println() => println();
	public void Println(string s) => println(s);
	private class ConsoleOutputProvider : OutputProvider { public void Print(string s) => Console.Write(s); public void Print(byte b) => Console.Write((char)b); public void Println() => Console.WriteLine(); }

	public class StringOutputProvider : OutputProvider
	{
		private readonly TextWriter _writer;

		public StringOutputProvider(StringWriter writer)
		{
			_writer = writer ?? throw new ArgumentNullException(nameof(writer));
		}

		public void Print(string s)
		{
			_writer.Write(s);
		}

		public void Print(byte b)
		{
			_writer.Write((char)b);
		}

		public void Println()
		{
			_writer.WriteLine();
		}
	}
}