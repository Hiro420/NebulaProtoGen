namespace unluac.decompile.statement
{
	public abstract class Statement
	{
		public string comment;
		public abstract void Print(Decompiler d, Output output);
		public virtual void PrintTail(Decompiler d, Output output) { Print(d, output); }
		public virtual bool BeginsWithParen() => false;
		public void AddComment(string c) { comment = c; }
		public static void PrintSequence(Decompiler d, Output output, System.Collections.Generic.IList<Statement> statements)
		{
			int n = statements.Count;
			for (int i = 0; i < n; i++)
			{
				bool last = (i + 1 == n);
				var stmt = statements[i];
				if (last) stmt.PrintTail(d, output); else stmt.Print(d, output);
				output.Println();
			}
		}
	}
}
