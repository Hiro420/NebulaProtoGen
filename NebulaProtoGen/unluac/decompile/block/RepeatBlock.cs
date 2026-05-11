using unluac.decompile.branch;
using unluac.decompile.statement;
using unluac.parse;

namespace unluac.decompile.block
{
	public class RepeatBlock : Block
	{
		private readonly Branch _branch;
		private readonly Registers _registers;
		private readonly List<Statement> _statements;

		public RepeatBlock(LFunction function, Branch branch, Registers registers) : base(function, branch.end, branch.begin)
		{
			_branch = branch;
			_registers = registers;
			_statements = new List<Statement>(branch.begin - branch.end + 1);
		}

		public override bool Breakable() => true;
		public override bool IsContainer() => true;

		public override void AddStatement(Statement statement)
		{
			_statements.Add(statement);
		}

		public override bool IsUnprotected() => false;
		public override int GetLoopback() { throw new System.InvalidOperationException(); }

		public override void Print(Decompiler d, Output output)
		{
			output.Print("repeat");
			output.Println();
			output.Indent();
			Statement.PrintSequence(d, output, _statements);
			output.Dedent();
			output.Print("until ");
			_branch.AsExpression(_registers).Print(d, output);
		}
	}
}
