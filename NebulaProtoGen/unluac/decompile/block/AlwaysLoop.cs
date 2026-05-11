using unluac.decompile.statement;
using unluac.parse;

namespace unluac.decompile.block
{
	public class AlwaysLoop : Block
	{
		private readonly List<Statement> _statements;

		public AlwaysLoop(LFunction function, int begin, int end) : base(function, begin, end)
		{
			_statements = new List<Statement>();
		}

		public override int ScopeEnd() => end - 2;
		public override bool Breakable() => true;
		public override bool IsContainer() => true;
		public override bool IsUnprotected() => true;
		public override int GetLoopback() => begin;

		public override void Print(Decompiler d, Output output)
		{
			output.Println("while true do");
			output.Indent();
			Statement.PrintSequence(d, output, _statements);
			output.Dedent();
			output.Print("end");
		}

		public override void AddStatement(Statement statement)
		{
			_statements.Add(statement);
		}
	}
}
