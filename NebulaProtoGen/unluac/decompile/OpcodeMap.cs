namespace unluac.decompile
{
	public class OpcodeMap
	{
		private readonly Op[] map;
		public OpcodeMap(int version)
		{
			if (version == 0x50)
			{
				map = new Op[] { Op.MOVE, Op.LOADK, Op.LOADBOOL, Op.LOADNIL, Op.GETUPVAL, Op.GETGLOBAL, Op.GETTABLE, Op.SETGLOBAL, Op.SETUPVAL, Op.SETTABLE, Op.NEWTABLE50, Op.SELF, Op.ADD, Op.SUB, Op.MUL, Op.DIV, Op.POW, Op.UNM, Op.NOT, Op.CONCAT, Op.JMP, Op.EQ, Op.LT, Op.LE, Op.TEST50, Op.CALL, Op.TAILCALL, Op.RETURN, Op.FORLOOP, Op.TFORLOOP, Op.TFORPREP, Op.SETLIST50, Op.SETLISTO, Op.CLOSE, Op.CLOSURE };
			}
			else if (version == 0x51)
			{
				map = new Op[] { Op.MOVE, Op.LOADK, Op.LOADBOOL, Op.LOADNIL, Op.GETUPVAL, Op.GETGLOBAL, Op.GETTABLE, Op.SETGLOBAL, Op.SETUPVAL, Op.SETTABLE, Op.NEWTABLE, Op.SELF, Op.ADD, Op.SUB, Op.MUL, Op.DIV, Op.MOD, Op.POW, Op.UNM, Op.NOT, Op.LEN, Op.CONCAT, Op.JMP, Op.EQ, Op.LT, Op.LE, Op.TEST, Op.TESTSET, Op.CALL, Op.TAILCALL, Op.RETURN, Op.FORLOOP, Op.FORPREP, Op.TFORLOOP, Op.SETLIST, Op.CLOSE, Op.CLOSURE, Op.VARARG };
			}
			else if (version == 0x52)
			{
				map = new Op[] { Op.MOVE, Op.LOADK, Op.LOADKX, Op.LOADBOOL, Op.LOADNIL, Op.GETUPVAL, Op.GETTABUP, Op.GETTABLE, Op.SETTABUP, Op.SETUPVAL, Op.SETTABLE, Op.NEWTABLE, Op.SELF, Op.ADD, Op.SUB, Op.MUL, Op.DIV, Op.MOD, Op.POW, Op.UNM, Op.NOT, Op.LEN, Op.CONCAT, Op.JMP, Op.EQ, Op.LT, Op.LE, Op.TEST, Op.TESTSET, Op.CALL, Op.TAILCALL, Op.RETURN, Op.FORLOOP, Op.FORPREP, Op.TFORCALL, Op.TFORLOOP, Op.SETLIST, Op.CLOSURE, Op.VARARG, Op.EXTRAARG };
			}
			else
			{ // 5.3+
				map = new Op[] { Op.MOVE, Op.LOADK, Op.LOADKX, Op.LOADBOOL, Op.LOADNIL, Op.GETUPVAL, Op.GETTABUP, Op.GETTABLE, Op.SETTABUP, Op.SETUPVAL, Op.SETTABLE, Op.NEWTABLE, Op.SELF, Op.ADD, Op.SUB, Op.MUL, Op.DIV, Op.MOD, Op.POW, Op.IDIV, Op.BAND, Op.BOR, Op.BXOR, Op.SHL, Op.SHR, Op.UNM, Op.BNOT, Op.NOT, Op.LEN, Op.CONCAT, Op.JMP, Op.EQ, Op.LT, Op.LE, Op.TEST, Op.TESTSET, Op.CALL, Op.TAILCALL, Op.RETURN, Op.FORLOOP, Op.FORPREP, Op.TFORCALL, Op.TFORLOOP, Op.SETLIST, Op.CLOSURE, Op.VARARG, Op.EXTRAARG };
			}
		}
		public Op? Get(int opNumber) => (opNumber >= 0 && opNumber < map.Length) ? map[opNumber] : null;
	}
}
