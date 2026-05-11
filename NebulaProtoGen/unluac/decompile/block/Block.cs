using unluac.decompile.operation;
using unluac.decompile.statement;
using unluac.parse;
namespace unluac.decompile.block
{
	public abstract class Block : Statement, IComparable<Block>
	{
		protected readonly LFunction function; public int begin; public int end; public bool loopRedirectAdjustment = false;
		protected Block(LFunction function, int begin, int end) { this.function = function; this.begin = begin; this.end = end; }
		public abstract void AddStatement(Statement statement);
		public bool Contains(Block block) => begin <= block.begin && end >= block.end;
		public bool Contains(int line) => begin <= line && line < end;
		public virtual int ScopeEnd() => end - 1;
		public abstract bool IsUnprotected();
		public abstract int GetLoopback();
		public abstract bool Breakable();
		public abstract bool IsContainer();
		public virtual int CompareTo(Block block) { if (this.begin < block.begin) return -1; else if (this.begin == block.begin) { if (this.end < block.end) return 1; else if (this.end == block.end) { if (this.IsContainer() && !block.IsContainer()) return -1; else if (!this.IsContainer() && block.IsContainer()) return 1; else return 0; } else return -1; } else return 1; }
		public virtual Operation Process(Decompiler d) { Statement statement = this; return new IdentityOperation(end - 1, statement); }

		public bool contains(Block b) => Contains(b);
		public bool contains(int line) => Contains(line);
		public int scopeEnd() => ScopeEnd();
		public bool isUnprotected() => IsUnprotected();
		public int getLoopback() => GetLoopback();
		public bool breakable() => Breakable();
		public bool isContainer() => IsContainer();
		public Operation process(Decompiler d) => Process(d);
		public void print(Decompiler d, Output output) => Print(d, output);
	}

	internal class IdentityOperation : operation.Operation
	{
		private readonly Statement _statement;
		public IdentityOperation(int line, Statement statement) : base(line) { _statement = statement; }
		public override Statement process(Registers r, Block block) { return _statement; }
	}
}
