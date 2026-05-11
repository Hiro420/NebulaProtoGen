using unluac.decompile.block;
using unluac.decompile.expression;
using unluac.decompile.statement;
using unluac.decompile.target;

namespace unluac.decompile.operation
{
	public class TableSet : Operation
	{
		private readonly Expression _table; private readonly Expression _key; private readonly Expression _value; private readonly bool _raw; private readonly int _updatedLine;
		public TableSet(int line, Expression table, Expression key, Expression value, bool raw, int updatedLine) : base(line) { _table = table; _key = key; _value = value; _raw = raw; _updatedLine = updatedLine; }
		public override Statement process(Registers r, Block block)
		{
			// Merge field sets into a table literal when possible
			// to avoid invalid statements like "{}.field = value".
			if (_table.IsTableLiteral() && (_value.IsMultiple() || _table.IsNewEntryAllowed()))
			{
				_table.AddEntry(new TableLiteral.Entry(_key, _value, !_raw, _updatedLine));
				return null; // merged into literal. no standalone assignment.
			}
			// Fallback: emit explicit assignment
			var assign = new Assignment();
			assign.AddLast(new TableTarget(_table, _key), _value);
			return assign;
		}
	}
}
