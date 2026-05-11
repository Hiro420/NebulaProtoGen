using unluac.parse;

namespace unluac.decompile
{
	public class Constant
	{
		private static readonly HashSet<string> reservedWords = new() { "and", "break", "do", "else", "elseif", "end", "false", "for", "function", "if", "in", "local", "nil", "not", "or", "repeat", "return", "then", "true", "until", "while" };
		private readonly int type; private readonly bool boolVal; private readonly LNumber? number; private readonly string? str;
		public Constant(int constant) { type = 2; boolVal = false; number = LNumber.MakeInteger(constant); str = null; }
		public Constant(LObject constant)
		{
			switch (constant)
			{
				case LNil: type = 0; boolVal = false; number = null; str = null; break;
				case LBoolean lb: type = 1; boolVal = ReferenceEquals(lb, LBoolean.LTRUE); number = null; str = null; break;
				case LNumber ln: type = 2; boolVal = false; number = ln; str = null; break;
				case LString ls: type = 3; boolVal = false; number = null; str = ls.Deref(); break;
				default: throw new ArgumentException("Illegal constant type: " + constant);
			}
		}
		public void Print(Decompiler d, Output output, bool braced)
		{
			switch (type)
			{
				case 0: output.Print("nil"); break;
				case 1: output.Print(boolVal ? "true" : "false"); break;
				case 2: output.Print(number!.ToString()); break;
				case 3: PrintString(d, output, braced); break;
				default: throw new InvalidOperationException();
			}
		}
		private void PrintString(Decompiler d, Output output, bool braced)
		{
			var s = str!; int newlines = 0, unprintable = 0; bool raw = d.GetConfiguration().rawstring; foreach (char c in s) { if (c == '\n') newlines++; else if ((c <= 31 && c != '\t') || c >= 127) unprintable++; }
			if (unprintable == 0 && (newlines > 1 || (newlines == 1 && s.IndexOf('\n') != s.Length - 1)))
			{
				int pipe = 0; if (s.Length > 0 && s[^1] == ']') pipe = 1; string pipeString = "]]"; while (s.Contains(pipeString)) { pipe++; pipeString = "]" + new string('=', pipe) + "]"; }
				if (braced) output.Print("("); output.Print("["); for (int i = 0; i < pipe; i++) output.Print("="); output.Print("["); int indent = output.GetIndentationLevel(); output.SetIndentationLevel(0); output.Println(); output.Print(s); output.Print("]"); for (int i = 0; i < pipe; i++) output.Print("="); output.Print("]"); if (braced) output.Print(")"); output.SetIndentationLevel(indent); return;
			}
			output.Print("\""); foreach (char c in s) { if (c <= 31 || c >= 127) { output.Print(EscapeChar(c, raw)); } else if (c == '"') output.Print("\\\""); else if (c == '\\') output.Print("\\\\"); else output.Print(c.ToString()); }
			output.Print("\"");
		}
		private string EscapeChar(char c, bool raw) { return c switch { (char)7 => "\\a", (char)8 => "\\b", (char)12 => "\\f", (char)10 => "\\n", (char)13 => "\\r", (char)9 => "\\t", (char)11 => "\\v", _ => (!raw || c <= 127) ? "\\" + ((int)c).ToString().PadLeft(3, '0') : ((byte)c).ToString() }; }
		public bool IsNil() => type == 0; public bool IsBoolean() => type == 1; public bool IsNumber() => type == 2; public bool IsInteger() => number!.Value() == System.Math.Round(number.Value()); public int AsInteger() { if (!IsInteger()) throw new InvalidOperationException(); return (int)number!.Value(); }
		public bool IsString() => type == 3; public bool IsIdentifier() { if (!IsString()) return false; var s = str!; if (reservedWords.Contains(s) || s.Length == 0) return false; char start = s[0]; if (start != '_' && !char.IsLetter(start)) return false; for (int i = 1; i < s.Length; i++) { char n = s[i]; if (char.IsLetterOrDigit(n) || n == '_') continue; return false; } return true; }
		public string AsName() { if (type != 3) throw new InvalidOperationException(); return str!; }
	}
}
