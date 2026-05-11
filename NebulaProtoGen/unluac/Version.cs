using unluac.decompile;
using unluac.parse;

namespace unluac
{
	public abstract class Version
	{

		public static readonly Version LUA50 = new Version50();
		public static readonly Version LUA51 = new Version51();
		public static readonly Version LUA52 = new Version52();
		public static readonly Version LUA53 = new Version53();

		// Exposed as public to match direct field access pattern from Java port (function.header.version.versionNumber)
		public readonly int versionNumber;

		protected Version(int versionNumber)
		{
			this.versionNumber = versionNumber;
		}

		public abstract LHeaderType GetLHeaderType();

		public OpcodeMap GetOpcodeMap() => new OpcodeMap(versionNumber);

		public abstract int GetOuterBlockScopeAdjustment();
		public abstract bool UsesOldLoadNilEncoding();
		public abstract bool UsesInlineUpvalueDeclarations();
		public abstract Op GetTForTarget();
		public abstract Op GetForTarget();
		public abstract bool IsBreakableLoopEnd(Op op);
		public abstract bool IsAllowedPreceedingSemicolon();
		public abstract bool IsEnvironmentTable(string name);
		public LHeaderType getLHeaderType() => GetLHeaderType();
		public OpcodeMap getOpcodeMap() => GetOpcodeMap();
		public int getOuterBlockScopeAdjustment() => GetOuterBlockScopeAdjustment();
		public bool usesOldLoadNilEncoding() => UsesOldLoadNilEncoding();
		public bool usesInlineUpvalueDeclarations() => UsesInlineUpvalueDeclarations();
		public Op getTForTarget() => GetTForTarget();
		public Op getForTarget() => GetForTarget();
		public bool isBreakableLoopEnd(Op op) => IsBreakableLoopEnd(op);
		public bool isAllowedPreceedingSemicolon() => IsAllowedPreceedingSemicolon();
		public bool isEnvironmentTable(string name) => IsEnvironmentTable(name);
	}

	class Version50 : Version
	{
		public Version50() : base(0x50) { }
		public override LHeaderType GetLHeaderType() => LHeaderType.TYPE50;
		public override int GetOuterBlockScopeAdjustment() => -1;
		public override bool UsesOldLoadNilEncoding() => true;
		public override bool UsesInlineUpvalueDeclarations() => true;
		public override Op GetTForTarget() => Op.TFORLOOP;
		public override Op GetForTarget() => Op.FORLOOP;
		public override bool IsBreakableLoopEnd(Op op) => op == Op.JMP || op == Op.FORLOOP;
		public override bool IsAllowedPreceedingSemicolon() => false;
		public override bool IsEnvironmentTable(string upvalue) => false;
	}

	class Version51 : Version
	{
		public Version51() : base(0x51) { }
		public override LHeaderType GetLHeaderType() => LHeaderType.TYPE51;
		public override int GetOuterBlockScopeAdjustment() => -1;
		public override bool UsesOldLoadNilEncoding() => true;
		public override bool UsesInlineUpvalueDeclarations() => true;
		public override Op GetTForTarget() => Op.TFORLOOP;
		public override Op GetForTarget() => Op.FORLOOP; // use FORLOOP as neutral target
		public override bool IsBreakableLoopEnd(Op op) => op == Op.JMP || op == Op.FORLOOP;
		public override bool IsAllowedPreceedingSemicolon() => false;
		public override bool IsEnvironmentTable(string upvalue) => false;
	}

	class Version52 : Version
	{
		public Version52() : base(0x52) { }
		public override LHeaderType GetLHeaderType() => LHeaderType.TYPE52;
		public override int GetOuterBlockScopeAdjustment() => 0;
		public override bool UsesOldLoadNilEncoding() => false;
		public override bool UsesInlineUpvalueDeclarations() => false;
		public override Op GetTForTarget() => Op.TFORCALL;
		public override Op GetForTarget() => Op.FORLOOP; // neutral
		public override bool IsBreakableLoopEnd(Op op) => op == Op.JMP || op == Op.FORLOOP || op == Op.TFORLOOP;
		public override bool IsAllowedPreceedingSemicolon() => true;
		public override bool IsEnvironmentTable(string name) => name == "_ENV";
	}

	class Version53 : Version
	{
		public Version53() : base(0x53) { }
		public override LHeaderType GetLHeaderType() => LHeaderType.TYPE53;
		public override int GetOuterBlockScopeAdjustment() => 0;
		public override bool UsesOldLoadNilEncoding() => false;
		public override bool UsesInlineUpvalueDeclarations() => false;
		public override Op GetTForTarget() => Op.TFORCALL;
		public override Op GetForTarget() => Op.FORLOOP; // neutral
		public override bool IsBreakableLoopEnd(Op op) => op == Op.JMP || op == Op.FORLOOP || op == Op.TFORLOOP;
		public override bool IsAllowedPreceedingSemicolon() => true;
		public override bool IsEnvironmentTable(string name) => name == "_ENV";
	}

}
