using unluac.decompile.branch;
using unluac.decompile.statement;
using unluac.parse;

namespace unluac.decompile.block
{
	public class WhileBlock : Block
	{
		private readonly Branch _branch;
		private readonly int _loopback;
		private readonly Registers _registers;
		private readonly List<Statement> _statements;

		public WhileBlock(LFunction function, Branch branch, int loopback, Registers registers)
		  : base(function, branch.begin, branch.end)
		{
			_branch = branch;
			_loopback = loopback;
			_registers = registers;
			_statements = new List<Statement>(branch.end - branch.begin + 1);
		}

		public override int ScopeEnd() => end - 2;
		public override bool Breakable() => true;
		public override bool IsContainer() => true;
		public override void AddStatement(Statement statement) { _statements.Add(statement); }
		public override bool IsUnprotected() => true;
		public override int GetLoopback() => _loopback;

		public override void Print(Decompiler d, Output output)
		{
			output.Print("while ");
			_branch.AsExpression(_registers).Print(d, output);
			output.Print(" do");
			output.Println();
			output.Indent();
			Statement.PrintSequence(d, output, _statements);
			output.Dedent();
			output.Print("end");
		}
	}
}
