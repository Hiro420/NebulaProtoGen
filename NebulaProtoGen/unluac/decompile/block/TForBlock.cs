using unluac.decompile.expression;
using unluac.decompile.statement;
using unluac.parse;

namespace unluac.decompile.block
{
	public class TForBlock : Block
	{
		private readonly int _register;
		private readonly int _length;
		private readonly Registers _registers;
		private readonly List<Statement> _statements;

		public TForBlock(LFunction function, int begin, int end, int register, int length, Registers registers)
		  : base(function, begin, end)
		{
			_register = register;
			_length = length;
			_registers = registers;
			_statements = new List<Statement>(end - begin + 1);
		}

		public override int ScopeEnd() => end - 3;
		public override bool Breakable() => true;
		public override bool IsContainer() => true;
		public override void AddStatement(Statement statement) { _statements.Add(statement); }
		public override bool IsUnprotected() => false;
		public override int GetLoopback() { throw new System.InvalidOperationException(); }

		public override void Print(Decompiler d, Output output)
		{
			output.Print("for ");
			if (function.header.version == Version.LUA50)
			{
				_registers.getTarget(_register + 2, begin - 1).Print(d, output);
				for (int r1 = _register + 3; r1 <= _register + 2 + _length; r1++)
				{
					output.Print(", ");
					_registers.getTarget(r1, begin - 1).Print(d, output);
				}
			}
			else
			{
				_registers.getTarget(_register + 3, begin - 1).Print(d, output);
				for (int r1 = _register + 4; r1 <= _register + 2 + _length; r1++)
				{
					output.Print(", ");
					_registers.getTarget(r1, begin - 1).Print(d, output);
				}
			}
			output.Print(" in ");
			Expression value = _registers.getValue(_register, begin - 1);
			value.Print(d, output);
			if (!value.IsMultiple())
			{
				output.Print(", ");
				value = _registers.getValue(_register + 1, begin - 1);
				value.Print(d, output);
				if (!value.IsMultiple())
				{
					output.Print(", ");
					value = _registers.getValue(_register + 2, begin - 1);
					value.Print(d, output);
				}
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
