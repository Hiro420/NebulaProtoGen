namespace unluac.decompile.expression
{
	public class TableLiteral : Expression
	{
		public class Entry
		{
			public readonly Expression key;
			public readonly Expression value;
			public readonly bool isList;
			public readonly int timestamp;
			public Entry(Expression key, Expression value, bool isList, int timestamp)
			{
				this.key = key; this.value = value; this.isList = isList; this.timestamp = timestamp;
			}
		}

		private readonly List<Entry> _entries;
		private bool _isObject = true;
		private bool _isList = true;
		private int _listLength = 1;
		private readonly int _capacity;
		// Tracks the variable name this literal was assigned to (if any)
		public string? AssignedName { get; private set; }

		public TableLiteral(int arraySize, int hashSize) : base(PRECEDENCE_ATOMIC)
		{
			_entries = new List<Entry>(arraySize + hashSize);
			_capacity = arraySize + hashSize;
		}

		public override int GetConstantIndex()
		{
			int idx = -1;
			foreach (var e in _entries)
			{
				idx = System.Math.Max(e.key.GetConstantIndex(), idx);
				idx = System.Math.Max(e.value.GetConstantIndex(), idx);
			}
			return idx;
		}

		public override void Print(Decompiler d, Output output)
		{
			if (_entries.Count == 0)
			{
				output.Print("{}");
				return;
			}
			bool lineBreak = _isList && _entries.Count > 5 || _isObject && _entries.Count > 2 || !_isObject;
			if (!lineBreak)
			{
				foreach (var e in _entries)
				{
					if (!e.value.IsBrief()) { lineBreak = true; break; }
				}
			}
			output.Print("{");
			if (lineBreak) { output.Println(); output.Indent(); }
			PrintEntry(d, 0, output);
			if (!_entries[0].value.IsMultiple())
			{
				for (int i = 1; i < _entries.Count; i++)
				{
					output.Print(",");
					if (lineBreak) output.Println(); else output.Print(" ");
					PrintEntry(d, i, output);
					if (_entries[i].value.IsMultiple()) break;
				}
			}
			if (lineBreak) { output.Println(); output.Dedent(); }
			output.Print("}");
		}

		private void PrintEntry(Decompiler d, int index, Output output)
		{
			var entry = _entries[index];
			var key = entry.key; var value = entry.value; bool isList = entry.isList;
			bool multiple = index + 1 >= _entries.Count || value.IsMultiple();
			if (isList && key.IsInteger() && _listLength == key.AsInteger())
			{
				if (multiple) value.PrintMultiple(d, output); else value.Print(d, output);
				_listLength++;
			}
			else if (_isObject && key.IsIdentifier())
			{
				output.Print(key.AsName());
				output.Print(" = ");
				value.Print(d, output);
			}
			else
			{
				output.Print("[");
				key.PrintBraced(d, output);
				output.Print("] = ");
				value.Print(d, output);
			}
		}

		public override bool IsTableLiteral() => true;
		public override bool IsUngrouped() => true;
		public override bool IsNewEntryAllowed() => _entries.Count < _capacity;
		public override void AddEntry(Entry entry)
		{
			_entries.Add(entry);
			_isObject = _isObject && (entry.isList || entry.key.IsIdentifier());
			_isList = _isList && entry.isList;
		}
		// Called when a RegisterSet assigns this literal to a local variable
		public void BindName(string name)
		{
			if (AssignedName == null) AssignedName = name;
		}
		public override bool IsBrief() => false;
	}
}
