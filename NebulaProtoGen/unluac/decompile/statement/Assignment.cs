using unluac.decompile.expression;
using unluac.decompile.target;

namespace unluac.decompile.statement
{
	public class Assignment : Statement
	{
		private readonly List<Target> _targets = new();
		private readonly List<Expression> _values = new();
		private int _declareLine = -1;

		public Assignment() { }
		public Assignment(Target t, Expression v) { AddLast(t, v); }

		public void AddLast(Target t, Expression v) { _targets.Add(t); _values.Add(v); }
		public void AddFirst(Target t, Expression v) { _targets.Insert(0, t); _values.Insert(0, v); }
		public void Declare(int line) { _declareLine = line; }
		public Target GetFirstTarget() => _targets[0];
		public Expression GetFirstValue() => _values[0];
		public int GetArity() => _targets.Count;

		public Target getFirstTarget() => GetFirstTarget();
		public Expression getFirstValue() => GetFirstValue();
		public int getArity() => GetArity();
		public void addLast(Target t, Expression v) => AddLast(t, v);
		public void addFirst(Target t, Expression v) => AddFirst(t, v);
		public void declare(int line) => Declare(line);

		public override void Print(Decompiler d, Output output)
		{
			if (_targets.Count == 0) return;
			// Deduplicate identical variable/global targets introduced by porting differences.
			// Keep the last value for each duplicate name.
			var indices = new System.Collections.Generic.List<int>(_targets.Count);
			var seen = new System.Collections.Generic.HashSet<string>();
			for (int i = _targets.Count - 1; i >= 0; i--)
			{
				string? key = null;
				var t = _targets[i];
				if (t is target.VariableTarget)
				{
					var expr = t.ToExpression();
					if (expr is expression.LocalVariable lv) key = lv.Declaration.name;
				}
				else if (t is target.GlobalTarget)
				{
					var expr = t.ToExpression();
					if (expr is expression.GlobalExpression ge) key = ge.AsName();
				}
				if (key == null)
				{
					// Non-variable/global targets rarely duplicate. keep all.
					indices.Add(i);
				}
				else if (!seen.Contains(key))
				{
					seen.Add(key); indices.Add(i);
				}
			}
			indices.Reverse();
			// Print targets
			_targets[indices[0]].Print(d, output);
			for (int k = 1; k < indices.Count; k++) { output.Print(", "); _targets[indices[k]].Print(d, output); }
			output.Print(" = ");
			// Collect matching values in same order
			var values = new System.Collections.Generic.List<expression.Expression>(indices.Count);
			foreach (var idx in indices) values.Add(_values[idx]);
			expression.Expression.PrintSequence(d, output, values, false, true);
		}
	}
}
