using unluac.decompile.expression;
using unluac.decompile.target;

namespace unluac.decompile
{
	public class Registers
	{
		public readonly int registers; public readonly int length;
		private readonly Declaration[][] _decls; private readonly Function _f; private readonly Expression[][] _values; private readonly int[][] _updated; private readonly bool[] _startedLines;

		public Registers(int registers, int length, Declaration[] declList, Function f)
		{
			this.registers = registers; this.length = length;
			// Allocate jagged arrays correctly in C#
			_decls = new Declaration[registers][];
			_values = new Expression[registers][];
			_updated = new int[registers][];
			for (int r = 0; r < registers; r++)
			{
				_decls[r] = new Declaration[length + 1];
				_values[r] = new Expression[length + 1];
				_updated[r] = new int[length + 1];
			}
			for (int i = 0; i < declList.Length; i++)
			{
				var decl = declList[i]; int reg = 0; while (_decls[reg][decl.begin] != null) reg++; decl.register = reg; for (int line = decl.begin; line <= decl.end; line++) _decls[reg][line] = decl;
			}
			for (int r = 0; r < registers; r++) { _values[r][0] = Expression.NIL; _updated[r][0] = 0; }
			_startedLines = new bool[length + 1]; Array.Fill(_startedLines, false); _f = f;
		}

		public bool IsAssignable(int register, int line) => IsLocal(register, line) && !_decls[register][line].forLoop;
		public bool IsLocal(int register, int line) { if (register < 0) return false; return _decls[register][line] != null; }
		public bool IsNewLocal(int register, int line) { var decl = _decls[register][line]; return decl != null && decl.begin == line && !decl.forLoop; }
		public List<Declaration> GetNewLocals(int line) { var list = new List<Declaration>(registers); for (int r = 0; r < registers; r++) if (IsNewLocal(r, line)) list.Add(GetDeclaration(r, line)); return list; }
		public Declaration GetDeclaration(int register, int line) => _decls[register][line];
		public void StartLine(int line) { _startedLines[line] = true; for (int r = 0; r < registers; r++) { _values[r][line] = _values[r][line - 1]; _updated[r][line] = _updated[r][line - 1]; } }
		public Expression GetExpression(int register, int line) { if (IsLocal(register, line - 1)) return new LocalVariable(GetDeclaration(register, line - 1)); return _values[register][line - 1]; }
		public Expression GetKExpression(int register, int line) { return _f.isConstant(register) ? _f.getConstantExpression(_f.constantIndex(register)) : GetExpression(register, line); }
		public Expression GetValue(int register, int line) => _values[register][line - 1];
		public int GetUpdated(int register, int line) => _updated[register][line];
		public void SetValue(int register, int line, Expression value) { _values[register][line] = value; _updated[register][line] = line; }
		public Target GetTarget(int register, int line) { if (!IsLocal(register, line)) throw new InvalidOperationException("No declaration exists in register " + register + " at line " + line); return new VariableTarget(_decls[register][line]); }
		public void SetInternalLoopVariable(int register, int begin, int end) { var decl = GetDeclaration(register, begin); if (decl == null) { decl = new Declaration("_FOR_", begin, end) { register = register }; NewDeclaration(decl, register, begin, end); } decl.forLoop = true; }
		public void SetExplicitLoopVariable(int register, int begin, int end) { var decl = GetDeclaration(register, begin); if (decl == null) { decl = new Declaration("_FORV_" + register + "_", begin, end) { register = register }; NewDeclaration(decl, register, begin, end); } decl.forLoopExplicit = true; }
		private void NewDeclaration(Declaration decl, int register, int begin, int end) { for (int line = begin; line <= end; line++) _decls[register][line] = decl; }

		public bool isAssignable(int register, int line) => IsAssignable(register, line);
		public bool isLocal(int register, int line) => IsLocal(register, line);
		public bool isNewLocal(int register, int line) => IsNewLocal(register, line);
		public List<Declaration> getNewLocals(int line) => GetNewLocals(line);
		public Declaration getDeclaration(int register, int line) => GetDeclaration(register, line);
		public void startLine(int line) => StartLine(line);
		public Expression getExpression(int register, int line) => GetExpression(register, line);
		public Expression getKExpression(int register, int line) => GetKExpression(register, line);
		public Expression getValue(int register, int line) => GetValue(register, line);
		public int getUpdated(int register, int line) => GetUpdated(register, line);
		public void setValue(int register, int line, Expression value) => SetValue(register, line, value);
		public Target getTarget(int register, int line) => GetTarget(register, line);
		public void setInternalLoopVariable(int register, int begin, int end) => SetInternalLoopVariable(register, begin, end);
		public void setExplicitLoopVariable(int register, int begin, int end) => SetExplicitLoopVariable(register, begin, end);
	}
}
