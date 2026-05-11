using unluac.decompile.branch;
using unluac.decompile.statement;
using unluac.parse;

namespace unluac.decompile.block
{
	public class IfThenElseBlock : Block
	{
		private readonly Branch _branch;
		private readonly int _loopback;
		private readonly Registers _registers;
		private readonly List<Statement> _statements;
		private readonly bool _emptyElse;
		public ElseEndBlock partner; // remains public for partner linkage

		public IfThenElseBlock(LFunction function, Branch branch, int loopback, bool emptyElse, Registers registers)
		  : base(function, branch.begin, branch.end)
		{
			_branch = branch;
			_loopback = loopback;
			_emptyElse = emptyElse;
			_registers = registers;
			_statements = new List<Statement>(branch.end - branch.begin + 1);
		}

		public override int CompareTo(Block block)
		{
			if (block == partner) return -1;
			return base.CompareTo(block);
		}

		public override bool Breakable() => false;
		public override bool IsContainer() => true;

		public override void AddStatement(Statement statement)
		{
			_statements.Add(statement);
		}

		public override int ScopeEnd() => end - 2;
		public override bool IsUnprotected() => true;
		public override int GetLoopback() => _loopback;

		public override void Print(Decompiler d, Output output)
		{
			output.Print("if ");
			_branch.AsExpression(_registers).Print(d, output);
			output.Print(" then");
			output.Println();
			output.Indent();
			// Handle empty then body disguised as break over else block
			if (_statements.Count == 1 && _statements[0] is Break b && b.target == _loopback)
			{
				output.Dedent();
				return;
			}
			Statement.PrintSequence(d, output, _statements);
			output.Dedent();
			if (_emptyElse)
			{
				output.Println("else");
				output.Println("end");
			}
		}
	}
}
