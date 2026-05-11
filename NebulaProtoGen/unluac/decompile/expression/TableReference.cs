namespace unluac.decompile.expression
{
	public class TableReference : Expression
	{
		private readonly Expression _table;
		private readonly Expression _key;

		public TableReference(Expression table, Expression key) : base(PRECEDENCE_ATOMIC)
		{
			_table = table;
			_key = key;
		}

		public override void Print(Decompiler d, Output output)
		{
			// - Detect environment table access (e.g. _ENV.identifier) and omit the dot/paren wrapping.
			// - Parenthesize "ungrouped" table expressions (like table literals) before member access.
			bool isGlobal = _table.IsEnvironmentTable(d) && _key.IsIdentifier();
			if (!isGlobal)
			{
				if (_table.IsUngrouped())
				{
					output.Print("(");
					_table.Print(d, output);
					output.Print(")");
				}
				else
				{
					_table.Print(d, output);
				}
			}
			if (_key.IsIdentifier())
			{
				if (!isGlobal)
				{
					output.Print(".");
				}
				output.Print(_key.AsName());
			}
			else
			{
				output.Print("[");
				_key.PrintBraced(d, output);
				output.Print("]");
			}
		}

		public override int GetConstantIndex()
		{
			int a = _table.GetConstantIndex();
			int b = _key.GetConstantIndex();
			return a >= b ? a : b;
		}

		public override bool IsMemberAccess() => _key.IsIdentifier();
		public override Expression GetTable() => _table;
		public override string GetField() => _key.AsName();
		public override bool BeginsWithParen() => _table.BeginsWithParen();
		public override void PrintMultiple(Decompiler d, Output output) => Print(d, output);
		public override bool IsDotChain() => _table.IsDotChain();
	}
}
