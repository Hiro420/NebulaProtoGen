using unluac.decompile.block;
using unluac.decompile.branch;
using unluac.decompile.expression;
using unluac.decompile.operation;
using unluac.decompile.statement;
using unluac.decompile.target;
using unluac.parse;

namespace unluac.decompile
{
	public class Decompiler
	{
		private readonly int _registers;
		private readonly int _length;
		public readonly Code code;
		private readonly Upvalues _upvalues;
		public readonly Declaration[] declList;
		protected Function fwrap;
		public LFunction function { get; protected set; }
		private readonly LFunction[] _functions;
		private readonly int _params;
		private readonly int _vararg;
		private readonly Op _tforTarget;
		private readonly Op _forTarget;

		public Decompiler(LFunction function) : this(function, null, -1) { }

		public Decompiler(LFunction function, Declaration[] parentDecls, int line)
		{
			fwrap = new Function(function);
			this.function = function;
			_registers = function.maximumStackSize;
			_length = function.code.Length;
			code = new Code(function);
			if (function.stripped)
			{
				declList = VariableFinder.process(this, function.numParams, function.maximumStackSize);
			}
			else if (function.locals.Length >= function.numParams)
			{
				declList = new Declaration[function.locals.Length];
				for (int i = 0; i < declList.Length; i++) declList[i] = new Declaration(function.locals[i]);
			}
			else
			{
				declList = new Declaration[function.numParams];
				for (int i = 0; i < declList.Length; i++) declList[i] = new Declaration("_ARG_" + i + "_", 0, _length - 1);
			}
			_upvalues = new Upvalues(function, parentDecls, line);
			_functions = function.functions;
			_params = function.numParams;
			_vararg = function.vararg;
			_tforTarget = function.header.version.getTForTarget();
			_forTarget = function.header.version.getForTarget();
		}

		public Version getVersion() => function.header.version;
		public Configuration getConfiguration() => function.header.config;
		// Uppercase alias for code referencing GetConfiguration()
		public Configuration GetConfiguration() => getConfiguration();

		private Registers r;
		private Block _outer;

		public void Decompile()
		{
			r = new Registers(_registers, _length, declList, fwrap);
			findReverseTargets();
			handleBranches(true);
			_outer = handleBranches(false);
			processSequence(1, _length);
		}

		public void Print(Output output) { handleInitialDeclares(output); _outer.print(this, output); }
		public void Print(OutputProvider provider) { Print(new Output(provider)); }
		public void Print() { Print(new Output()); }

		private void handleInitialDeclares(Output output) { var initdecls = new List<Declaration>(declList.Length); for (int i = _params + (_vararg & 1); i < declList.Length; i++) { if (declList[i].begin == 0) initdecls.Add(declList[i]); } if (initdecls.Count > 0) { output.print("local "); output.print(initdecls[0].name); for (int i = 1; i < initdecls.Count; i++) { output.print(", "); output.print(initdecls[i].name); } output.println(); } }

		private int fb2int50(int fb) { return (fb & 7) << (fb >> 3); }
		private int fb2int(int fb) { int exponent = (fb >> 3) & 0x1F; return exponent == 0 ? fb : ((fb & 7) + 8) << (exponent - 1); }

		private List<Operation> processLine(int line)
		{
			var operations = new List<Operation>(); int A = code.A(line); int B = code.B(line); int C = code.C(line); int Bx = code.Bx(line); switch (code.op(line))
			{
				case Op.MOVE: operations.Add(new RegisterSet(line, A, r.getExpression(B, line))); break;
				case Op.LOADK: operations.Add(new RegisterSet(line, A, fwrap.getConstantExpression(Bx))); break;
				case Op.LOADBOOL: operations.Add(new RegisterSet(line, A, new ConstantExpression(new Constant(B != 0 ? new LBoolean(true) : new LBoolean(false)), -1))); break;
				case Op.LOADNIL: { int maximum; if (function.header.version.usesOldLoadNilEncoding()) maximum = B; else maximum = A + B; while (A <= maximum) { operations.Add(new RegisterSet(line, A, Expression.NIL)); A++; } break; }
				case Op.GETUPVAL: operations.Add(new RegisterSet(line, A, _upvalues.getExpression(B))); break;
				case Op.GETTABUP: operations.Add(new RegisterSet(line, A, new TableReference(_upvalues.getExpression(B), r.getKExpression(C, line)))); break;
				case Op.GETGLOBAL: operations.Add(new RegisterSet(line, A, fwrap.getGlobalExpression(Bx))); break;
				case Op.GETTABLE: operations.Add(new RegisterSet(line, A, new TableReference(r.getExpression(B, line), r.getKExpression(C, line)))); break;
				case Op.SETUPVAL: operations.Add(new UpvalueSet(line, _upvalues.getName(B), r.getExpression(A, line))); break;
				case Op.SETTABUP: operations.Add(new TableSet(line, _upvalues.getExpression(A), r.getKExpression(B, line), r.getKExpression(C, line), true, line)); break;
				case Op.SETGLOBAL: operations.Add(new GlobalSet(line, fwrap.getGlobalName(Bx), r.getExpression(A, line))); break;
				case Op.SETTABLE: operations.Add(new TableSet(line, r.getExpression(A, line), r.getKExpression(B, line), r.getKExpression(C, line), true, line)); break;
				case Op.NEWTABLE: operations.Add(new RegisterSet(line, A, new TableLiteral(fb2int(B), fb2int(C)))); break;
				case Op.NEWTABLE50: operations.Add(new RegisterSet(line, A, new TableLiteral(fb2int50(B), 1 << C))); break;
				case Op.SELF: { Expression common = r.getExpression(B, line); operations.Add(new RegisterSet(line, A + 1, common)); operations.Add(new RegisterSet(line, A, new TableReference(common, r.getKExpression(C, line)))); break; }
				case Op.ADD: operations.Add(new RegisterSet(line, A, Expression.makeADD(r.getKExpression(B, line), r.getKExpression(C, line)))); break;
				case Op.SUB: operations.Add(new RegisterSet(line, A, Expression.makeSUB(r.getKExpression(B, line), r.getKExpression(C, line)))); break;
				case Op.MUL: operations.Add(new RegisterSet(line, A, Expression.makeMUL(r.getKExpression(B, line), r.getKExpression(C, line)))); break;
				case Op.DIV: operations.Add(new RegisterSet(line, A, Expression.makeDIV(r.getKExpression(B, line), r.getKExpression(C, line)))); break;
				case Op.MOD: operations.Add(new RegisterSet(line, A, Expression.makeMOD(r.getKExpression(B, line), r.getKExpression(C, line)))); break;
				case Op.POW: operations.Add(new RegisterSet(line, A, Expression.makePOW(r.getKExpression(B, line), r.getKExpression(C, line)))); break;
				case Op.IDIV: operations.Add(new RegisterSet(line, A, Expression.makeIDIV(r.getKExpression(B, line), r.getKExpression(C, line)))); break;
				case Op.BAND: operations.Add(new RegisterSet(line, A, Expression.makeBAND(r.getKExpression(B, line), r.getKExpression(C, line)))); break;
				case Op.BOR: operations.Add(new RegisterSet(line, A, Expression.makeBOR(r.getKExpression(B, line), r.getKExpression(C, line)))); break;
				case Op.BXOR: operations.Add(new RegisterSet(line, A, Expression.makeBXOR(r.getKExpression(B, line), r.getKExpression(C, line)))); break;
				case Op.SHL: operations.Add(new RegisterSet(line, A, Expression.makeSHL(r.getKExpression(B, line), r.getKExpression(C, line)))); break;
				case Op.SHR: operations.Add(new RegisterSet(line, A, Expression.makeSHR(r.getKExpression(B, line), r.getKExpression(C, line)))); break;
				case Op.UNM: operations.Add(new RegisterSet(line, A, Expression.makeUNM(r.getExpression(B, line)))); break;
				case Op.NOT: operations.Add(new RegisterSet(line, A, Expression.makeNOT(r.getExpression(B, line)))); break;
				case Op.LEN: operations.Add(new RegisterSet(line, A, Expression.makeLEN(r.getExpression(B, line)))); break;
				case Op.BNOT: operations.Add(new RegisterSet(line, A, Expression.makeBNOT(r.getExpression(B, line)))); break;
				case Op.CONCAT: { Expression value = r.getExpression(C, line); while (C-- > B) { value = Expression.makeCONCAT(r.getExpression(C, line), value); } operations.Add(new RegisterSet(line, A, value)); break; }
				case Op.JMP: case Op.EQ: case Op.LT: case Op.LE: case Op.TEST: case Op.TESTSET: case Op.TEST50: case Op.FORLOOP: case Op.FORPREP: case Op.TFORPREP: case Op.TFORCALL: case Op.TFORLOOP: /* handled later */ break;
				case Op.CALL: { bool multiple = (C >= 3 || C == 0); if (B == 0) B = _registers - A; if (C == 0) C = _registers - A + 1; Expression fn = r.getExpression(A, line); var args = new Expression[B - 1]; for (int reg = A + 1; reg <= A + B - 1; reg++) args[reg - A - 1] = r.getExpression(reg, line); var value = new FunctionCall(fn, args, multiple); if (C == 1) operations.Add(new CallOperation(line, value)); else { if (C == 2 && !multiple) operations.Add(new RegisterSet(line, A, value)); else { for (int reg = A; reg <= A + C - 2; reg++) operations.Add(new RegisterSet(line, reg, value)); } } break; }
				case Op.TAILCALL: { if (B == 0) B = _registers - A; Expression fn = r.getExpression(A, line); var args = new Expression[B - 1]; for (int reg = A + 1; reg <= A + B - 1; reg++) args[reg - A - 1] = r.getExpression(reg, line); var value = new FunctionCall(fn, args, true); operations.Add(new ReturnOperation(line, value)); skip[line + 1] = true; break; }
				case Op.RETURN: { if (B == 0) B = _registers - A + 1; var values = new Expression[B - 1]; for (int reg = A; reg <= A + B - 2; reg++) values[reg - A] = r.getExpression(reg, line); operations.Add(new ReturnOperation(line, values)); break; }
				case Op.SETLIST50: case Op.SETLISTO: { Expression table = r.getValue(A, line); int n = Bx % 32; for (int i = 1; i <= n + 1; i++) { operations.Add(new TableSet(line, table, new ConstantExpression(new Constant(Bx - n + i), -1), r.getExpression(A + i, line), false, r.getUpdated(A + i, line))); } break; }
				case Op.SETLIST: { if (C == 0) { C = code.codepoint(line + 1); skip[line + 1] = true; } if (B == 0) B = _registers - A - 1; Expression table = r.getValue(A, line); for (int i = 1; i <= B; i++) { operations.Add(new TableSet(line, table, new ConstantExpression(new Constant((C - 1) * 50 + i), -1), r.getExpression(A + i, line), false, r.getUpdated(A + i, line))); } break; }
				case Op.CLOSE: break;
				case Op.CLOSURE: { LFunction fsub = _functions[Bx]; operations.Add(new RegisterSet(line, A, new ClosureExpression(fsub, declList, line + 1))); if (function.header.version.usesInlineUpvalueDeclarations()) { for (int i = 0; i < fsub.numUpvalues; i++) { skip[line + 1 + i] = true; } } break; }
				case Op.VARARG: { bool multiple = (B != 2); if (B == 1) throw new InvalidOperationException(); if (B == 0) B = _registers - A + 1; Expression value = new Vararg(B - 1, multiple); for (int reg = A; reg <= A + B - 2; reg++) operations.Add(new RegisterSet(line, reg, value)); break; }
				default: throw new InvalidOperationException("Illegal instruction: " + code.op(line));
			}
			return operations;
		}

		private bool[] skip; // lines to skip
		private bool[] reverseTarget; // backward jump targets
		private void findReverseTargets() { reverseTarget = new bool[_length + 1]; for (int line = 1; line <= _length; line++) { if (code.op(line) == Op.JMP && code.sBx(line) < 0) { reverseTarget[line + 1 + code.sBx(line)] = true; } } }

		private Assignment processOperation(Operation operation, int line, int nextLine, Block block)
		{
			Assignment assign = null; bool wasMultiple = false; var stmt = operation.process(r, block); if (stmt != null) { if (stmt is Assignment a) { assign = a; if (!assign.GetFirstValue().IsMultiple()) block.AddStatement(stmt); else wasMultiple = true; } else { block.AddStatement(stmt); } if (assign != null) { while (nextLine < block.end && isMoveIntoTarget(nextLine)) { var target = getMoveIntoTargetTarget(nextLine, line + 1); var value = getMoveIntoTargetValue(nextLine, line + 1); assign.AddFirst(target, value); skip[nextLine] = true; nextLine++; } if (wasMultiple && !assign.GetFirstValue().IsMultiple()) block.AddStatement(stmt); } }
			return assign;
		}

		private void processSequence(int begin, int end)
		{
			int blockIndex = 1; var blockStack = new unluac.util.Stack<Block>(); blockStack.push(blocks[0]); skip = new bool[end + 1]; for (int line = begin; line <= end; line++)
			{
				Operation blockHandler = null; while (blockStack.peek().end <= line) { var b = blockStack.pop(); blockHandler = b.process(this); if (blockHandler != null) break; }
				if (blockHandler == null) { while (blockIndex < blocks.Count && blocks[blockIndex].begin <= line) blockStack.push(blocks[blockIndex++]); }
				var block = blockStack.peek(); r.startLine(line);
				if (skip[line]) { var newLocals = r.getNewLocals(line); if (newLocals.Count > 0) { var assign = new Assignment(); assign.Declare(newLocals[0].begin); foreach (var decl in newLocals) { assign.AddLast(new VariableTarget(decl), r.getValue(decl.register, line)); } blockStack.peek().AddStatement(assign); } continue; }
				var operations = processLine(line); var newLocals2 = r.getNewLocals(blockHandler == null ? line : line - 1); Assignment assign2 = null;
				if (blockHandler == null)
				{
					if (code.op(line) == Op.LOADNIL) { assign2 = new Assignment(); int count = 0; foreach (var op in operations) { var set = (RegisterSet)op; op.process(r, block); if (r.isAssignable(set.register, set.line)) { assign2.AddLast(r.getTarget(set.register, set.line), set.value); count++; } } if (count > 0) block.AddStatement(assign2); }
					else if (code.op(line) == Op.TFORPREP) { newLocals2.Clear(); }
					else { foreach (var op in operations) { var temp = processOperation(op, line, line + 1, block); if (temp != null) assign2 = temp; } if (assign2 != null && assign2.GetFirstValue().IsMultiple()) block.AddStatement(assign2); }
				}
				else { assign2 = processOperation(blockHandler, line, line, block); }
				if (assign2 != null) { if (newLocals2.Count > 0) { assign2.Declare(newLocals2[0].begin); foreach (var decl in newLocals2) { assign2.AddLast(new VariableTarget(decl), r.getValue(decl.register, line + 1)); } } }
				if (blockHandler == null) { if (assign2 != null) { } else if (newLocals2.Count > 0 && code.op(line) != Op.FORPREP) { if (code.op(line) != Op.JMP || code.op(line + 1 + code.sBx(line)) != _tforTarget) { var a = new Assignment(); a.Declare(newLocals2[0].begin); foreach (var decl in newLocals2) a.AddLast(new VariableTarget(decl), r.getValue(decl.register, line)); blockStack.peek().AddStatement(a); } } }
				if (blockHandler != null) { line--; continue; }
			}
		}

		private bool isMoveIntoTarget(int line) { switch (code.op(line)) { case Op.MOVE: return r.isAssignable(code.A(line), line) && !r.isLocal(code.B(line), line); case Op.SETUPVAL: case Op.SETGLOBAL: return !r.isLocal(code.A(line), line); case Op.SETTABLE: case Op.SETTABUP: { int C = code.C(line); if (fwrap.isConstant(C)) return false; else return !r.isLocal(C, line); } default: return false; } }
		private Target getMoveIntoTargetTarget(int line, int previous) { switch (code.op(line)) { case Op.MOVE: return r.getTarget(code.A(line), line); case Op.SETUPVAL: return new UpvalueTarget(_upvalues.getName(code.B(line))); case Op.SETGLOBAL: return new GlobalTarget(fwrap.getGlobalName(code.Bx(line))); case Op.SETTABLE: return new TableTarget(r.getExpression(code.A(line), previous), r.getKExpression(code.B(line), previous)); case Op.SETTABUP: { int A = code.A(line); int B = code.B(line); return new TableTarget(_upvalues.getExpression(A), r.getKExpression(B, previous)); } default: throw new InvalidOperationException(); } }
		private Expression getMoveIntoTargetValue(int line, int previous) { int A = code.A(line); int B = code.B(line); int C = code.C(line); switch (code.op(line)) { case Op.MOVE: return r.getValue(B, previous); case Op.SETUPVAL: case Op.SETGLOBAL: return r.getExpression(A, previous); case Op.SETTABLE: case Op.SETTABUP: if (fwrap.isConstant(C)) throw new InvalidOperationException(); else return r.getExpression(C, previous); default: throw new InvalidOperationException(); } }

		private List<Block> blocks;
		private OuterBlock handleBranches(bool first)
		{
			var oldBlocks = blocks; blocks = new List<Block>(); var outer = new OuterBlock(function, _length); blocks.Add(outer); var isBreak = new bool[_length + 1]; var loopRemoved = new bool[_length + 1]; if (!first)
			{
				foreach (var blk in oldBlocks) { if (blk is AlwaysLoop) blocks.Add(blk); if (blk is Break br) { blocks.Add(br); isBreak[blk.begin] = true; } }
				var delete = new List<Block>(); foreach (var blk in blocks) { if (blk is AlwaysLoop) { foreach (var b2 in blocks) { if (blk != b2) { if (blk.begin == b2.begin) { if (blk.end < b2.end) { delete.Add(blk); loopRemoved[blk.end - 1] = true; } else { delete.Add(b2); loopRemoved[b2.end - 1] = true; } } } } } }
				foreach (var d in delete) blocks.Remove(d);
			}
			skip = new bool[_length + 1]; var stack = new unluac.util.Stack<Branch>(); bool reduce = false; bool testset = false; int testsetend = -1;
			for (int line = 1; line <= _length; line++)
			{
				if (!skip[line])
				{
					switch (code.op(line))
					{
						case Op.EQ: { var node = new EQNode(code.B(line), code.C(line), code.A(line) != 0, line, line + 2, line + 2 + code.sBx(line + 1)); stack.push(node); skip[line + 1] = true; if (code.op(node.end) == Op.LOADBOOL) { if (code.C(node.end) != 0) { node.isCompareSet = true; node.setTarget = code.A(node.end); } else if (node.end - 1 >= 1 && code.op(node.end - 1) == Op.LOADBOOL) { if (code.C(node.end - 1) != 0) { node.isCompareSet = true; node.setTarget = code.A(node.end); } } } continue; }
						case Op.LT: { var node = new LTNode(code.B(line), code.C(line), code.A(line) != 0, line, line + 2, line + 2 + code.sBx(line + 1)); stack.push(node); skip[line + 1] = true; if (code.op(node.end) == Op.LOADBOOL) { if (code.C(node.end) != 0) { node.isCompareSet = true; node.setTarget = code.A(node.end); } else if (node.end - 1 >= 1 && code.op(node.end - 1) == Op.LOADBOOL) { if (code.C(node.end - 1) != 0) { node.isCompareSet = true; node.setTarget = code.A(node.end); } } } continue; }
						case Op.LE: { var node = new LENode(code.B(line), code.C(line), code.A(line) != 0, line, line + 2, line + 2 + code.sBx(line + 1)); stack.push(node); skip[line + 1] = true; if (code.op(node.end) == Op.LOADBOOL) { if (code.C(node.end) != 0) { node.isCompareSet = true; node.setTarget = code.A(node.end); } else if (node.end - 1 >= 1 && code.op(node.end - 1) == Op.LOADBOOL) { if (code.C(node.end - 1) != 0) { node.isCompareSet = true; node.setTarget = code.A(node.end); } } } continue; }
						case Op.TEST: stack.push(new TestNode(code.A(line), code.C(line) != 0, line, line + 2, line + 2 + code.sBx(line + 1))); skip[line + 1] = true; continue;
						case Op.TESTSET: testset = true; testsetend = line + 2 + code.sBx(line + 1); stack.push(new TestSetNode(code.A(line), code.B(line), code.C(line) != 0, line, line + 2, line + 2 + code.sBx(line + 1))); skip[line + 1] = true; continue;
						case Op.TEST50: if (code.A(line) == code.B(line)) { stack.push(new TestNode(code.A(line), code.C(line) != 0, line, line + 2, line + 2 + code.sBx(line + 1))); } else { testset = true; testsetend = line + 2 + code.sBx(line + 1); stack.push(new TestSetNode(code.A(line), code.B(line), code.C(line) != 0, line, line + 2, line + 2 + code.sBx(line + 1))); } skip[line + 1] = true; continue;
						case Op.JMP:
							{
								reduce = true; int tline = line + 1 + code.sBx(line); if (tline >= 2 && code.op(tline - 1) == Op.LOADBOOL && code.C(tline - 1) != 0) { stack.push(new TrueNode(code.A(tline - 1), false, line, line + 1, tline)); skip[line + 1] = true; }
								else if (code.op(tline) == _tforTarget && !skip[tline]) { int A = code.A(tline); int C = code.C(tline); if (C == 0) throw new InvalidOperationException(); r.setInternalLoopVariable(A, tline, line + 1); r.setInternalLoopVariable(A + 1, tline, line + 1); r.setInternalLoopVariable(A + 2, tline, line + 1); for (int idx = 1; idx <= C; idx++) r.setExplicitLoopVariable(A + 2 + idx, line, tline + 2); skip[tline] = true; skip[tline + 1] = true; blocks.Add(new TForBlock(function, line + 1, tline + 2, A, C, r)); }
								else if (code.op(tline) == _forTarget && !skip[tline]) { int A = code.A(tline); r.setInternalLoopVariable(A, tline, line + 1); r.setInternalLoopVariable(A + 1, tline, line + 1); r.setInternalLoopVariable(A + 2, tline, line + 1); skip[tline] = true; blocks.Add(new ForBlock(function, line + 1, tline + 1, A, r)); }
								else if (code.sBx(line) == 2 && code.op(line + 1) == Op.LOADBOOL && code.C(line + 1) != 0) { blocks.Add(new BooleanIndicator(function, line)); }
								else if (code.op(tline) == Op.JMP && code.sBx(tline) + tline == line) { if (first) blocks.Add(new AlwaysLoop(function, line, tline + 1)); skip[tline] = true; }
								else { if (first || loopRemoved[line] || reverseTarget[line + 1]) { if (!isBreak[line]) { if (tline > line) { isBreak[line] = true; blocks.Add(new Break(function, line, tline)); } else { var enclosing = enclosingBreakableBlock(line); if (enclosing != null && enclosing.breakable() && code.op(enclosing.end) == Op.JMP && code.sBx(enclosing.end) + enclosing.end + 1 == tline) { isBreak[line] = true; blocks.Add(new Break(function, line, enclosing.end)); } else { blocks.Add(new AlwaysLoop(function, tline, line + 1)); } } } } }
								break;
							}
						case Op.FORPREP: reduce = true; blocks.Add(new ForBlock(function, line + 1, line + 2 + code.sBx(line), code.A(line), r)); skip[line + 1 + code.sBx(line)] = true; r.setInternalLoopVariable(code.A(line), line, line + 2 + code.sBx(line)); r.setInternalLoopVariable(code.A(line) + 1, line, line + 2 + code.sBx(line)); r.setInternalLoopVariable(code.A(line) + 2, line, line + 2 + code.sBx(line)); r.setExplicitLoopVariable(code.A(line) + 3, line, line + 2 + code.sBx(line)); break;
						case Op.TFORPREP: { reduce = true; int tline = line + 1 + code.sBx(line); int A = code.A(tline); int C = code.C(tline); r.setInternalLoopVariable(A, tline, line + 1); r.setInternalLoopVariable(A + 1, tline, line + 1); r.setInternalLoopVariable(A + 2, tline, line + 1); for (int idx = 1; idx <= C; idx++) r.setExplicitLoopVariable(A + 2 + idx, line, tline + 2); skip[tline] = true; skip[tline + 1] = true; blocks.Add(new TForBlock(function, line + 1, tline + 2, A, C, r)); break; }
						default: reduce = isStatement(line); break;
					}
				}
				if ((line + 1) <= _length && reverseTarget[line + 1]) reduce = true;
				if (testset && testsetend == line + 1) reduce = true;
				if (stack.isEmpty()) reduce = false;
				if (reduce)
				{
					reduce = false; var conditions = new unluac.util.Stack<Branch>(); var backups = new unluac.util.Stack<unluac.util.Stack<Branch>>(); do
					{
						bool isAssignNode = stack.peek() is TestSetNode; int assignEnd = stack.peek().end; bool compareCorrect = false;
						if (stack.peek() is TrueNode) { isAssignNode = true; compareCorrect = true; if (code.C(assignEnd) != 0) assignEnd += 2; else assignEnd += 1; }
						else if (stack.peek().isCompareSet) { if (code.op(stack.peek().begin) != Op.LOADBOOL || code.C(stack.peek().begin) == 0) { isAssignNode = true; if (code.C(assignEnd) != 0) assignEnd += 2; else assignEnd += 1; compareCorrect = true; } }
						else if (assignEnd - 3 >= 1 && code.op(assignEnd - 2) == Op.LOADBOOL && code.C(assignEnd - 2) != 0 && code.op(assignEnd - 3) == Op.JMP && code.sBx(assignEnd - 3) == 2) { if (stack.peek() is TestNode tn) { if (tn.test == code.A(assignEnd - 2)) isAssignNode = true; } }
						else if (assignEnd - 2 >= 1 && code.op(assignEnd - 1) == Op.LOADBOOL && code.C(assignEnd - 1) != 0 && code.op(assignEnd - 2) == Op.JMP && code.sBx(assignEnd - 2) == 2) { if (stack.peek() is TestNode) { isAssignNode = true; assignEnd += 1; } }
						else if (assignEnd - 1 >= 1 && code.op(assignEnd) == Op.LOADBOOL && code.C(assignEnd) != 0 && code.op(assignEnd - 1) == Op.JMP && code.sBx(assignEnd - 1) == 2) { if (stack.peek() is TestNode) { isAssignNode = true; assignEnd += 2; } }
						else if (assignEnd - 1 >= 1 && r.isLocal(getAssignment(assignEnd - 1), assignEnd - 1) && assignEnd > stack.peek().line) { var decl = r.getDeclaration(getAssignment(assignEnd - 1), assignEnd - 1); if (decl.begin == assignEnd - 1 && decl.end > assignEnd - 1) isAssignNode = true; }
						unluac.util.Stack<Branch> backupStack;
						if (!compareCorrect && assignEnd - 1 == stack.peek().begin && code.op(stack.peek().begin) == Op.LOADBOOL && code.C(stack.peek().begin) != 0) { backupStack = null; int begin2 = stack.peek().begin; assignEnd = begin2 + 2; int target2 = code.A(begin2); conditions.push(popCompareSetCondition(stack, assignEnd, target2)); conditions.peek().setTarget = target2; conditions.peek().end = assignEnd; conditions.peek().begin = begin2; }
						else if (isAssignNode) { backupStack = null; int target = stack.peek().setTarget; int begin = stack.peek().begin; conditions.push(popSetCondition(stack, assignEnd, target)); conditions.peek().setTarget = target; conditions.peek().end = assignEnd; conditions.peek().begin = begin; }
						else { backupStack = new unluac.util.Stack<Branch>(); conditions.push(popCondition(stack)); backupStack.reverse(); }
						backups.push(backupStack);
					} while (!stack.isEmpty());
					do
					{
						var cond = conditions.pop(); var backup2 = backups.pop(); int bt = breakTarget(cond.begin); bool breakable = (bt >= 1); if (breakable && code.op(bt) == Op.JMP && bt != cond.end) bt += 1 + code.sBx(bt);
						if (breakable && bt == cond.end) { var immediate = enclosingBlock(cond.begin); var breakableEnclosing = enclosingBreakableBlock(cond.begin); int loopstart = immediate.end; if (immediate == breakableEnclosing) loopstart--; for (int iline = loopstart; iline >= Math.Max(cond.begin, immediate.begin); iline--) { if (code.op(iline) == Op.JMP && iline + 1 + code.sBx(iline) == bt) { cond.end = iline; break; } } }
						bool hasTail = cond.end >= 2 && code.op(cond.end - 1) == Op.JMP; int tail = hasTail ? cond.end + code.sBx(cond.end - 1) : -1; int originalTail = tail; var enclosing2 = enclosingUnprotectedBlock(cond.begin); if (enclosing2 != null) { if (enclosing2.GetLoopback() == cond.end) { cond.end = enclosing2.end - 1; hasTail = cond.end >= 2 && code.op(cond.end - 1) == Op.JMP; tail = hasTail ? cond.end + code.sBx(cond.end - 1) : -1; } if (hasTail && enclosing2.GetLoopback() == tail) { tail = enclosing2.end - 1; } }
						if (cond.isSet) { bool empty = cond.begin == cond.end; if (code.op(cond.begin) == Op.JMP && code.sBx(cond.begin) == 2 && code.op(cond.begin + 1) == Op.LOADBOOL && code.C(cond.begin + 1) != 0) empty = true; blocks.Add(new SetBlock(function, cond, cond.setTarget, line, cond.begin, cond.end, empty, r)); }
						else if (code.op(cond.begin) == Op.LOADBOOL && code.C(cond.begin) != 0) { int begin3 = cond.begin; int target = code.A(begin3); if (code.B(begin3) == 0) cond = cond.invert(); blocks.Add(new CompareBlock(function, begin3, begin3 + 2, target, cond)); }
						else if (cond.end < cond.begin) { if (isBreak[cond.end - 1]) { skip[cond.end - 1] = true; blocks.Add(new WhileBlock(function, cond.invert(), originalTail, r)); } else { blocks.Add(new RepeatBlock(function, cond, r)); } }
						else if (hasTail)
						{
							var endOp = code.op(cond.end - 2); bool isEndCondJump = endOp == Op.EQ || endOp == Op.LE || endOp == Op.LT || endOp == Op.TEST || endOp == Op.TESTSET || endOp == Op.TEST50; if (tail > cond.end || (tail == cond.end && !isEndCondJump)) { var op = code.op(tail - 1); int sbx = code.sBx(tail - 1); int loopback2 = tail + sbx; bool isBreakableLoopEnd = function.header.version.isBreakableLoopEnd(op); if (isBreakableLoopEnd && loopback2 <= cond.begin && !isBreak[tail - 1]) { blocks.Add(new IfThenEndBlock(function, cond, backup2, r)); } else { skip[cond.end - 1] = true; bool emptyElse = tail == cond.end; var ifthen = new IfThenElseBlock(function, cond, originalTail, emptyElse, r); blocks.Add(ifthen); if (!emptyElse) { var elseend = new ElseEndBlock(function, cond.end, tail); blocks.Add(elseend); } } }
							else { int loopback = tail; bool existsStatement = false; for (int sl = loopback; sl < cond.begin; sl++) { if (!skip[sl] && isStatement(sl)) { existsStatement = true; break; } } if (loopback >= cond.begin || existsStatement) { blocks.Add(new IfThenEndBlock(function, cond, backup2, r)); } else { skip[cond.end - 1] = true; blocks.Add(new WhileBlock(function, cond, originalTail, r)); } }
						}
						else { blocks.Add(new IfThenEndBlock(function, cond, backup2, r)); }
					} while (!conditions.isEmpty());
				}
			}
			// add do..end blocks for decls whose scope not matched
			foreach (var decl in declList) { if (!decl.forLoop && !decl.forLoopExplicit) { bool needs = true; foreach (var block in blocks) { if (block.contains(decl.begin)) { if (block.scopeEnd() == decl.end) { needs = false; break; } } } if (needs) { blocks.Add(new DoEndBlock(function, decl.begin, decl.end + 1)); } } }
			// remove breaks that are else jumps
			for (int i = blocks.Count - 1; i >= 0; i--) { var b = blocks[i]; if (skip[b.begin] && b is Break) blocks.RemoveAt(i); }
			blocks.Sort((a, b) => a.CompareTo(b)); backup = null; return outer;
		}

		private int breakTarget(int line) { int tline = int.MaxValue; foreach (var block in blocks) { if (block.Breakable() && block.contains(line)) tline = Math.Min(tline, block.end); } return tline == int.MaxValue ? -1 : tline; }
		private Block enclosingBlock(int line) { var outer = blocks[0]; Block enclosing = outer; for (int i = 1; i < blocks.Count; i++) { var next = blocks[i]; if (next.IsContainer() && enclosing.contains(next) && next.contains(line) && !next.loopRedirectAdjustment) enclosing = next; } return enclosing; }
		private Block enclosingBreakableBlock(int line) { var outer = blocks[0]; Block enclosing = outer; for (int i = 1; i < blocks.Count; i++) { var next = blocks[i]; if (enclosing.contains(next) && next.contains(line) && next.Breakable() && !next.loopRedirectAdjustment) enclosing = next; } return enclosing == outer ? null : enclosing; }
		private Block enclosingUnprotectedBlock(int line) { var outer = blocks[0]; Block enclosing = outer; for (int i = 1; i < blocks.Count; i++) { var next = blocks[i]; if (enclosing.contains(next) && next.contains(line) && next.IsUnprotected() && !next.loopRedirectAdjustment) enclosing = next; } return enclosing == outer ? null : enclosing; }

		private static unluac.util.Stack<Branch> backup;
		public Branch popCondition(unluac.util.Stack<Branch> stack) { Branch branch = stack.pop(); if (backup != null) backup.push(branch); if (branch is TestSetNode) throw new InvalidOperationException(); int begin = branch.begin; if (code.op(branch.begin) == Op.JMP) begin += 1 + code.sBx(branch.begin); while (!stack.isEmpty()) { var next = stack.peek(); if (next is TestSetNode) break; if (next.end == begin) { branch = new OrBranch(popCondition(stack).invert(), branch); } else if (next.end == branch.end) { branch = new AndBranch(popCondition(stack), branch); } else break; } return branch; }
		public Branch popSetCondition(unluac.util.Stack<Branch> stack, int assignEnd, int target) { stack.push(new AssignNode(assignEnd - 1, assignEnd, assignEnd)); var rtn = _helper_popSetCondition(stack, false, assignEnd, target); return rtn; }
		public Branch popCompareSetCondition(unluac.util.Stack<Branch> stack, int assignEnd, int target) { Branch top = stack.pop(); bool invert = false; if (code.B(top.begin) == 0) invert = true; top.begin = assignEnd; top.end = assignEnd; stack.push(top); var rtn = _helper_popSetCondition(stack, invert, assignEnd, target); return rtn; }
		private int _adjustLine(int line, int target) { int testline = line; while (testline >= 1 && code.op(testline) == Op.LOADBOOL && (target == -1 || code.A(testline) == target)) testline--; if (testline == line) return testline; testline++; if (code.C(testline) != 0) return testline + 2; else return testline + 1; }
		private Branch _helper_popSetCondition(unluac.util.Stack<Branch> stack, bool invert, int assignEnd, int target)
		{
			Branch branch = stack.pop(); int begin = branch.begin; int end = branch.end; if (invert) branch = branch.invert(); begin = _adjustLine(begin, target); end = _adjustLine(end, target); int btarget = branch.setTarget; while (!stack.isEmpty())
			{
				Branch next = stack.peek(); bool ninvert; int nend = next.end; if (code.op(nend) == Op.LOADBOOL && (target == -1 || code.A(nend) == target)) { ninvert = code.B(nend) != 0; nend = _adjustLine(nend, target); } else if (next is TestSetNode tsn) { ninvert = tsn.invert; } else if (next is TestNode tn) { ninvert = tn.invert; } else { ninvert = false; if (nend >= assignEnd) break; }
				int addr = (ninvert == invert) ? end : begin; if (addr == nend) { if (ninvert) branch = new OrBranch(_helper_popSetCondition(stack, ninvert, assignEnd, target), branch); else branch = new AndBranch(_helper_popSetCondition(stack, ninvert, assignEnd, target), branch); branch.end = nend; } else { if (!(branch is TestSetNode)) { stack.push(branch); branch = popCondition(stack); } break; }
			}
			branch.isSet = true; branch.setTarget = btarget; return branch;
		}

		private bool isStatement(int line) => isStatement(line, -1);
		private bool isStatement(int line, int testRegister)
		{
			switch (code.op(line))
			{
				case Op.MOVE: case Op.LOADK: case Op.LOADBOOL: case Op.GETUPVAL: case Op.GETTABUP: case Op.GETGLOBAL: case Op.GETTABLE: case Op.NEWTABLE: case Op.NEWTABLE50: case Op.ADD: case Op.SUB: case Op.MUL: case Op.DIV: case Op.MOD: case Op.POW: case Op.UNM: case Op.NOT: case Op.LEN: case Op.IDIV: case Op.BAND: case Op.BOR: case Op.BXOR: case Op.SHL: case Op.SHR: case Op.BNOT: case Op.CONCAT: case Op.CLOSURE: return r.isLocal(code.A(line), line) || code.A(line) == testRegister;
				case Op.LOADNIL: for (int reg = code.A(line); reg <= code.B(line); reg++) { if (r.isLocal(reg, line)) return true; } return false;
				case Op.SETGLOBAL: case Op.SETUPVAL: case Op.SETTABUP: case Op.SETTABLE: case Op.JMP: case Op.TAILCALL: case Op.RETURN: case Op.FORLOOP: case Op.FORPREP: case Op.TFORPREP: case Op.TFORCALL: case Op.TFORLOOP: case Op.CLOSE: return true;
				case Op.SELF: return r.isLocal(code.A(line), line) || r.isLocal(code.A(line) + 1, line);
				case Op.EQ: case Op.LT: case Op.LE: case Op.TEST: case Op.TESTSET: case Op.TEST50: case Op.SETLIST: case Op.SETLISTO: case Op.SETLIST50: return false;
				case Op.CALL: { int a = code.A(line); int c = code.C(line); if (c == 1) return true; if (c == 0) c = _registers - a + 1; for (int reg = a; reg < a + c - 1; reg++) { if (r.isLocal(reg, line)) return true; } return (c == 2 && a == testRegister); }
				case Op.VARARG: { int a = code.A(line); int b = code.B(line); if (b == 0) b = _registers - a + 1; for (int reg = a; reg < a + b - 1; reg++) { if (r.isLocal(reg, line)) return true; } return false; }
				default: throw new InvalidOperationException("Illegal opcode: " + code.op(line));
			}
		}
		private int getAssignment(int line)
		{
			switch (code.op(line))
			{
				case Op.MOVE: case Op.LOADK: case Op.LOADBOOL: case Op.GETUPVAL: case Op.GETTABUP: case Op.GETGLOBAL: case Op.GETTABLE: case Op.NEWTABLE: case Op.NEWTABLE50: case Op.ADD: case Op.SUB: case Op.MUL: case Op.DIV: case Op.MOD: case Op.POW: case Op.UNM: case Op.NOT: case Op.LEN: case Op.IDIV: case Op.BAND: case Op.BOR: case Op.BXOR: case Op.SHL: case Op.SHR: case Op.BNOT: case Op.CONCAT: case Op.CLOSURE: return code.A(line);
				case Op.LOADNIL: if (code.A(line) == code.B(line)) return code.A(line); else return -1;
				case Op.SETGLOBAL: case Op.SETUPVAL: case Op.SETTABUP: case Op.SETTABLE: case Op.JMP: case Op.TAILCALL: case Op.RETURN: case Op.FORLOOP: case Op.FORPREP: case Op.TFORCALL: case Op.TFORLOOP: case Op.CLOSE: case Op.SELF: case Op.EQ: case Op.LT: case Op.LE: case Op.TEST: case Op.TESTSET: case Op.SETLIST: case Op.SETLIST50: case Op.SETLISTO: return -1;
				case Op.CALL: return (code.C(line) == 2) ? code.A(line) : -1;
				case Op.VARARG: return (code.C(line) == 2) ? code.B(line) : -1;
				default: throw new InvalidOperationException("Illegal opcode: " + code.op(line));
			}
		}
	}
}
