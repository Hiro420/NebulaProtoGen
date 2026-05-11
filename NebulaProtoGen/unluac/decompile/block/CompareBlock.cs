using unluac.decompile.branch;
using unluac.decompile.expression;
using unluac.decompile.operation;
using unluac.decompile.statement;
using unluac.parse;

namespace unluac.decompile.block
{
	public class CompareBlock : Block
	{
		private readonly int _target;
		private readonly Branch _branch;

		public CompareBlock(LFunction function, int begin, int end, int target, Branch branch)
		  : base(function, begin, end)
		{
			_target = target;
			_branch = branch;
		}

		public override bool IsContainer() => false;
		public override bool Breakable() => false;
		public override void AddStatement(Statement statement) { /* intentionally empty */ }
		public override bool IsUnprotected() => false;
		public override int GetLoopback() { throw new System.InvalidOperationException(); }

		public override void Print(Decompiler d, Output output)
		{
			output.Print("-- unhandled compare assign");
		}

		public override Operation Process(Decompiler d)
		{
			return new CompareAssignOperation(end - 1, _target, _branch);
		}

		private class CompareAssignOperation : Operation
		{
			private readonly int _target;
			private readonly Branch _branch;
			public CompareAssignOperation(int line, int target, Branch branch) : base(line)
			{
				_target = target; _branch = branch;
			}
			public override Statement process(Registers r, Block block)
			{
				Expression expr = _branch.AsExpression(r);
				if (r.isLocal(_target, line))
				{
					return new Assignment(r.getTarget(_target, line), expr);
				}
				r.setValue(_target, line, expr);
				return null;
			}
		}
	}
}
