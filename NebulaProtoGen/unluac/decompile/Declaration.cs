using unluac.parse;

namespace unluac.decompile
{
	public class Declaration
	{
		public readonly string name; public readonly int begin; public readonly int end; public int register; public bool forLoop; public bool forLoopExplicit;
		public Declaration(LLocal local) { name = local.ToString(); begin = local.start; end = local.end; }
		public Declaration(string name, int begin, int end) { this.name = name; this.begin = begin; this.end = end; }
	}
}
