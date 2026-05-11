using unluac.decompile.branch;
using unluac.decompile.expression;
using unluac.decompile.operation;
using unluac.decompile.statement;
using unluac.parse;

namespace unluac.decompile.block
{
	public class IfThenEndBlock : Block
	{
		private readonly Branch _branch;
		private readonly unluac.util.Stack<Branch> _stack;
		private readonly Registers _registers;
		private readonly List<Statement> _statements;

		public IfThenEndBlock(LFunction function, Branch branch, Registers registers) : this(function, branch, null, registers) { }

		public IfThenEndBlock(LFunction function, Branch branch, unluac.util.Stack<Branch> stack, Registers registers)
		  : base(function, branch.begin == branch.end ? branch.begin - 1 : branch.begin, branch.begin == branch.end ? branch.begin - 1 : branch.end)
		{
			_branch = branch;
			_stack = stack;
			_registers = registers;
			_statements = new List<Statement>(branch.end - branch.begin + 1);
		}

		public override void AddStatement(Statement statement)
		{
			_statements.Add(statement);
		}

		public override bool Breakable() => false;
		public override bool IsContainer() => true;
		public override bool IsUnprotected() => false;
		public override int GetLoopback() { throw new System.InvalidOperationException(); }

		public override void Print(Decompiler d, Output output)
		{
			output.Print("if ");
			_branch.AsExpression(_registers).Print(d, output);
			output.Print(" then");
			output.Println();
			output.Indent();
			Statement.PrintSequence(d, output, _statements);
			output.Dedent();
			output.Print("end");
		}

		public override Operation Process(Decompiler d)
		{
			if (_statements.Count == 1)
			{
				var stmt = _statements[0];
				if (stmt is Assignment assign && assign.getArity() == 1)
				{
					if (_branch is TestNode node)
					{
						var decl = _registers.getDeclaration(node.test, node.line);
						if (assign.getFirstTarget().isDeclaration(decl))
						{
							Expression expr = node.invert
							  ? new BinaryExpression("or", new LocalVariable(decl), assign.getFirstValue(), Expression.PRECEDENCE_OR, Expression.ASSOCIATIVITY_NONE)
							  : new BinaryExpression("and", new LocalVariable(decl), assign.getFirstValue(), Expression.PRECEDENCE_AND, Expression.ASSOCIATIVITY_NONE);

							return new MergeAssignmentOperation(end - 1, assign.getFirstTarget(), expr);
						}
					}
				}
			}
			else if (_statements.Count == 0 && _stack != null)
			{
				int test = _branch.GetRegister();
				if (test < 0)
				{
					for (int reg = 0; reg < _registers.registers; reg++)
					{
						if (_registers.getUpdated(reg, _branch.end - 1) >= _branch.begin)
						{
							if (test >= 0)
							{
								test = -1;
								break;
							}
							test = reg;
						}
					}
				}
				if (test >= 0 && _registers.getUpdated(test, _branch.end - 1) >= _branch.begin)
				{
					Expression right = _registers.getValue(test, _branch.end);
					Branch setb = d.popSetCondition(_stack, _stack.Peek().end, test);
					setb.UseExpression(right);
					return new SetConditionOperation(end - 1, test, setb, _branch);
				}
			}
			return base.Process(d);
		}

		private class MergeAssignmentOperation : Operation
		{
			private readonly target.Target _target;
			private readonly Expression _value;
			public MergeAssignmentOperation(int line, target.Target target, Expression value) : base(line)
			{
				_target = target; _value = value;
			}
			public override Statement process(Registers r, Block block)
			{
				return new Assignment(_target, _value);
			}
		}

		private class SetConditionOperation : Operation
		{
			private readonly int _testReg;
			private readonly Branch _setBranch;
			private readonly Branch _originalBranch;
			public SetConditionOperation(int line, int testReg, Branch setBranch, Branch originalBranch) : base(line)
			{
				_testReg = testReg; _setBranch = setBranch; _originalBranch = originalBranch;
			}
			public override Statement process(Registers r, Block block)
			{
				r.setValue(_testReg, _originalBranch.end - 1, _setBranch.AsExpression(r));
				return null;
			}
		}
	}
}
