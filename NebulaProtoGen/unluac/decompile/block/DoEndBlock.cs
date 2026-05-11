using unluac.decompile.statement;
using unluac.parse;

namespace unluac.decompile.block
{
	public class DoEndBlock : Block
	{
		private readonly List<Statement> _statements;

		public DoEndBlock(LFunction function, int begin, int end) : base(function, begin, end)
		{
			_statements = new List<Statement>(end - begin + 1);
		}

		public override void AddStatement(Statement statement)
		{
			_statements.Add(statement);
		}

		public override bool Breakable() => false;
		public override bool IsContainer() => true;
		public override bool IsUnprotected() => false;
		public override int GetLoopback() { throw new System.InvalidOperationException(); }

		public override void Print(Decompiler d, Output output)
		{
			output.Println("do");
			output.Indent();
			Statement.PrintSequence(d, output, _statements);
			output.Dedent();
			output.Print("end");
		}
	}
}
