using unluac.decompile.expression;

namespace unluac.decompile.target
{
	public class TableTarget : Target
	{
		private readonly Expression _table; private readonly Expression _key;
		public TableTarget(Expression table, Expression key) { _table = table; _key = key; }
		public override void Print(Decompiler d, Output output)
		{
			// If the table is a literal bound to a variable, print the variable name instead of '{}'
			if (_table.IsTableLiteral() && _table is TableLiteral tl && tl.AssignedName != null)
			{
				output.Print(tl.AssignedName);
			}
			else
			{
				_table.Print(d, output);
			}
			if (_key.IsIdentifier())
			{
				output.Print(".");
				output.Print(_key.AsName());
			}
			else
			{
				output.Print("[");
				_key.Print(d, output);
				output.Print("]");
			}
		}
		public override bool IsTableTarget() => true;
		public override Expression ToExpression() => new TableReference(_table, _key);
	}
}
