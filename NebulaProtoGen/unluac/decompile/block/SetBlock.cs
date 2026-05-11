using unluac.decompile.branch;
using unluac.decompile.expression;
using unluac.decompile.operation;
using unluac.decompile.statement;
using unluac.decompile.target;
using unluac.parse;

namespace unluac.decompile.block
{
	public class SetBlock : Block
	{
		public readonly int target;
		public readonly Branch branch;

		private Assignment _assign;
		private readonly Registers _registers;
		private readonly bool _empty;
		private bool _finalize = false;

		public SetBlock(LFunction function, Branch branch, int target, int line, int begin, int end, bool empty, Registers registers)
		  : base(function, begin, end)
		{
			_empty = empty;
			if (begin == end) this.begin -= 1;
			this.target = target;
			this.branch = branch;
			_registers = registers;
		}

		public override void AddStatement(Statement statement)
		{
			if (!_finalize && statement is Assignment assignment)
			{
				_assign = assignment;
			}
			else if (statement is BooleanIndicator)
			{
				_finalize = true;
			}
		}

		public override bool IsUnprotected() => false;
		public override int GetLoopback() { throw new InvalidOperationException(); }

		public override void Print(Decompiler d, Output output)
		{
			if (_assign != null && _assign.GetFirstTarget() != null)
			{
				var assignOut = new Assignment(_assign.GetFirstTarget(), GetValue());
				assignOut.Print(d, output);
			}
			else
			{
				output.Print("-- unhandled set block");
				output.Println();
			}
		}

		public override bool Breakable() => false;
		public override bool IsContainer() => false;

		public void UseAssignment(Assignment assignment)
		{
			_assign = assignment;
			branch.UseExpression(assignment.GetFirstValue());
		}

		public Expression GetValue() => branch.AsExpression(_registers);

		public override Operation Process(Decompiler d)
		{
			if (_empty)
			{
				var expression = _registers.GetExpression(branch.setTarget, end);
				branch.UseExpression(expression);
				return new RegisterSet(end - 1, branch.setTarget, branch.AsExpression(_registers));
			}
			else if (_assign != null)
			{
				branch.UseExpression(_assign.GetFirstValue());
				var targetLocal = _assign.GetFirstTarget();
				var value = GetValue();
				return new SimpleAssignmentOperation(end - 1, targetLocal, value);
			}
			else
			{
				return new ComplexSetOperation(end - 1, this, d);
			}
		}

		private class SimpleAssignmentOperation : Operation
		{
			private readonly Target _target;
			private readonly Expression _value;
			public SimpleAssignmentOperation(int line, Target target, Expression value) : base(line)
			{
				_target = target; _value = value;
			}
			public override Statement process(Registers r, Block block)
			{
				return new Assignment(_target, _value);
			}
		}

		private class ComplexSetOperation : Operation
		{
			private readonly SetBlock _parent;
			private readonly Decompiler _decompiler;
			public ComplexSetOperation(int line, SetBlock parent, Decompiler decompiler) : base(line)
			{
				_parent = parent; _decompiler = decompiler;
			}
			public override Statement process(Registers r, Block block)
			{
				Expression expr = null;
				for (int reg = 0; reg < r.registers; reg++)
				{
					if (r.GetUpdated(reg, _parent.branch.end - 1) == _parent.branch.end - 1)
					{
						expr = r.GetValue(reg, _parent.branch.end);
						break;
					}
				}

				if (_decompiler.code.op(_parent.branch.end - 2) == Op.LOADBOOL && _decompiler.code.C(_parent.branch.end - 2) != 0)
				{
					int targetLocal = _decompiler.code.A(_parent.branch.end - 2);
					if (_decompiler.code.op(_parent.branch.end - 3) == Op.JMP && _decompiler.code.sBx(_parent.branch.end - 3) == 2)
					{
						expr = r.GetValue(targetLocal, _parent.branch.end - 2);
					}
					else
					{
						expr = r.GetValue(targetLocal, _parent.branch.begin);
					}
					_parent.branch.UseExpression(expr);
					if (r.IsLocal(targetLocal, _parent.branch.end - 1))
					{
						return new Assignment(r.GetTarget(targetLocal, _parent.branch.end - 1), _parent.branch.AsExpression(r));
					}
					r.SetValue(targetLocal, _parent.branch.end - 1, _parent.branch.AsExpression(r));
				}
				else if (expr != null && _parent.target >= 0)
				{
					_parent.branch.UseExpression(expr);
					if (r.IsLocal(_parent.target, _parent.branch.end - 1))
					{
						return new Assignment(r.GetTarget(_parent.target, _parent.branch.end - 1), _parent.branch.AsExpression(r));
					}
					r.SetValue(_parent.target, _parent.branch.end - 1, _parent.branch.AsExpression(r));
				}
				else
				{
					Console.WriteLine("-- fail " + (_parent.branch.end - 1));
					Console.WriteLine(expr);
					Console.WriteLine(_parent.target);
				}
				return null;
			}
		}
	}
}
