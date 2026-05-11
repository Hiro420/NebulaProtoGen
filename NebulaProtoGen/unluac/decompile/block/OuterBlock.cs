using unluac.decompile.statement;
using unluac.parse;

namespace unluac.decompile.block
{
	public class OuterBlock : Block
	{
		private readonly List<Statement> _statements;

		public OuterBlock(LFunction function, int length)
		  : base(function, 0, length + 1)
		{
			_statements = new List<Statement>(length);
		}

		public override void AddStatement(Statement statement)
		{
			_statements.Add(statement);
		}

		public override bool Breakable() => false;
		public override bool IsContainer() => true;
		public override bool IsUnprotected() => false;
		public override int GetLoopback() { throw new System.InvalidOperationException(); }
		public override int ScopeEnd() => (end - 1) + function.header.version.getOuterBlockScopeAdjustment();

		public override void Print(Decompiler d, Output output)
		{
			int last = _statements.Count - 1;
			if (last < 0 || !(_statements[last] is Return))
			{
				throw new System.InvalidOperationException(last >= 0 ? _statements[last].ToString() : "Missing implicit return statement");
			}
			_statements.RemoveAt(last); // remove implicit return
			Statement.PrintSequence(d, output, _statements);
		}
	}
}
