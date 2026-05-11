using unluac.decompile.statement;
using unluac.parse;

namespace unluac.decompile.block
{
	public class ElseEndBlock : Block
	{
		private readonly List<Statement> _statements;
		public IfThenElseBlock partner;

		public ElseEndBlock(LFunction function, int begin, int end) : base(function, begin, end)
		{
			_statements = new List<Statement>(end - begin + 1);
		}

		public override int CompareTo(Block block)
		{
			if (block == partner)
			{
				return 1;
			}
			return base.CompareTo(block);
		}

		public override bool Breakable() => false;
		public override bool IsContainer() => true;

		public override void AddStatement(Statement statement)
		{
			_statements.Add(statement);
		}

		public override bool IsUnprotected() => false;
		public override int GetLoopback() { throw new System.InvalidOperationException(); }

		public override void Print(Decompiler d, Output output)
		{
			if (_statements.Count == 1 && _statements[0] is IfThenEndBlock)
			{
				output.Print("else");
				_statements[0].Print(d, output);
			}
			else if (_statements.Count == 2 && _statements[0] is IfThenElseBlock && _statements[1] is ElseEndBlock)
			{
				output.Print("else");
				_statements[0].Print(d, output);
				_statements[1].Print(d, output);
			}
			else
			{
				output.Print("else");
				output.Println();
				output.Indent();
				Statement.PrintSequence(d, output, _statements);
				output.Dedent();
				output.Print("end");
			}
		}
	}
}
