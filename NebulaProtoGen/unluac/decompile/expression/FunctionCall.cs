namespace unluac.decompile.expression
{
	public class FunctionCall : Expression
	{
		private readonly Expression _function;
		private readonly Expression[] _arguments;
		private readonly bool _multiple;

		public FunctionCall(Expression function, Expression[] arguments, bool multiple) : base(PRECEDENCE_ATOMIC)
		{
			_function = function;
			_arguments = arguments;
			_multiple = multiple;
		}

		public override int GetConstantIndex()
		{
			int index = _function.GetConstantIndex();
			foreach (var arg in _arguments)
			{
				int c = arg.GetConstantIndex();
				if (c > index) index = c;
			}
			return index;
		}

		public override bool IsMultiple() => _multiple;

		public override void PrintMultiple(Decompiler d, Output output)
		{
			if (!_multiple)
			{
				output.Print("(");
				Print(d, output);
				output.Print(")");
			}
			else
			{
				Print(d, output);
			}
		}

		private bool IsMethodCall() => _function.IsMemberAccess() && _arguments.Length > 0 && _function.GetTable() == _arguments[0];

		public override bool BeginsWithParen()
		{
			if (IsMethodCall())
			{
				var obj = _function.GetTable();
				return obj.IsUngrouped() || obj.BeginsWithParen();
			}
			return _function.IsUngrouped() || _function.BeginsWithParen();
		}

		public override void Print(Decompiler d, Output output)
		{
			var args = new List<Expression>(_arguments.Length);
			if (IsMethodCall())
			{
				var obj = _function.GetTable();
				if (obj.IsUngrouped())
				{
					output.Print("(");
					obj.Print(d, output);
					output.Print(")");
				}
				else
				{
					obj.Print(d, output);
				}
				output.Print(":");
				output.Print(_function.GetField());
				for (int i = 1; i < _arguments.Length; i++) args.Add(_arguments[i]);
			}
			else
			{
				if (_function.IsUngrouped())
				{
					output.Print("(");
					_function.Print(d, output);
					output.Print(")");
				}
				else
				{
					_function.Print(d, output);
				}
				for (int i = 0; i < _arguments.Length; i++) args.Add(_arguments[i]);
			}
			output.Print("(");
			Expression.PrintSequence(d, output, args, false, true);
			output.Print(")");
		}
	}
}
