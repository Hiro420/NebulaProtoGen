using unluac.decompile.expression;
using unluac.decompile.statement;
using unluac.parse;

namespace unluac.decompile.block
{
	public class ForBlock : Block
	{
		private readonly int _register;
		private readonly Registers _registers;
		private readonly List<Statement> _statements;

		public ForBlock(LFunction function, int begin, int end, int register, Registers registers)
		  : base(function, begin, end)
		{
			_register = register;
			_registers = registers;
			_statements = new List<Statement>(end - begin + 1);
		}

		public override int ScopeEnd() => end - 2;
		public override void AddStatement(Statement statement) { _statements.Add(statement); }
		public override bool Breakable() => true;
		public override bool IsContainer() => true;
		public override bool IsUnprotected() => false;
		public override int GetLoopback() { throw new System.InvalidOperationException(); }

		public override void Print(Decompiler d, Output output)
		{
			output.Print("for ");
			if (function.header.version == Version.LUA50)
			{
				_registers.getTarget(_register, begin - 1).Print(d, output);
			}
			else
			{
				_registers.getTarget(_register + 3, begin - 1).Print(d, output);
			}
			output.Print(" = ");
			if (function.header.version == Version.LUA50)
			{
				_registers.getValue(_register, begin - 2).Print(d, output);
			}
			else
			{
				_registers.getValue(_register, begin - 1).Print(d, output);
			}
			output.Print(", ");
			_registers.getValue(_register + 1, begin - 1).Print(d, output);
			Expression step = _registers.getValue(_register + 2, begin - 1);
			if (!step.IsInteger() || step.AsInteger() != 1)
			{
				output.Print(", ");
				step.Print(d, output);
			}
			output.Print(" do");
			output.Println();
			output.Indent();
			Statement.PrintSequence(d, output, _statements);
			output.Dedent();
			output.Print("end");
		}
	}
}
