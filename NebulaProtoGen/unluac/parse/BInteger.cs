using System.Numerics;

namespace unluac.parse
{
	public class BInteger : BObject
	{
		private readonly BigInteger? big;
		private readonly int n;

		private static BigInteger? MAX_INT = null;
		private static BigInteger? MIN_INT = null;

		public BInteger(BInteger b)
		{
			big = b.big; n = b.n;
		}
		public BInteger(int n) { this.n = n; big = null; }
		public BInteger(BigInteger big)
		{
			this.big = big; n = 0;
			if (MAX_INT == null) { MAX_INT = new BigInteger(int.MaxValue); MIN_INT = new BigInteger(int.MinValue); }
		}
		public int AsInt()
		{
			if (big == null) return n;
			// Use static Compare to avoid any nullable/extension method ambiguity
			if (BigInteger.Compare(big.Value, MAX_INT!.Value) > 0 || BigInteger.Compare(big.Value, MIN_INT!.Value) < 0) throw new InvalidOperationException("Integer size outside supported range.");
			return (int)big.Value;
		}
		public void Iterate(Action thunk)
		{
			if (big == null)
			{
				int i = n; while (i-- != 0) thunk();
			}
			else
			{
				BigInteger i = big!.Value; while (i.Sign > 0) { thunk(); i -= BigInteger.One; }
			}
		}
	}
}