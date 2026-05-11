namespace unluac.util
{
	public enum ByteOrder { BigEndian, LittleEndian }

	public class ByteBuffer
	{
		private readonly byte[] _data;
		private int _pos;
		private int _mark = -1;
		public ByteOrder Order { get; private set; } = ByteOrder.LittleEndian;

		public ByteBuffer(byte[] data) { _data = data; _pos = 0; }
		public int Position => _pos;
		public int Remaining => _data.Length - _pos;
		public int Length => _data.Length;

		public void OrderSet(ByteOrder order) => Order = order;
		public void Rewind() { _pos = 0; }
		public void Mark() { _mark = _pos; }
		public void Reset() { if (_mark < 0) throw new InvalidOperationException("Mark not set"); _pos = _mark; }

		private void Require(int count) { if (_pos + count > _data.Length) throw new IndexOutOfRangeException("Buffer underflow"); }

		public byte Get() { Require(1); return _data[_pos++]; }
		public void Get(byte[] dest) { Require(dest.Length); Array.Copy(_data, _pos, dest, 0, dest.Length); _pos += dest.Length; }

		public short GetShort()
		{
			Require(2);
			short value = Order == ByteOrder.LittleEndian
			  ? (short)(_data[_pos] | (_data[_pos + 1] << 8))
			  : (short)((_data[_pos] << 8) | _data[_pos + 1]);
			_pos += 2; return value;
		}
		public int GetInt()
		{
			Require(4);
			int value = Order == ByteOrder.LittleEndian
			  ? (_data[_pos] | (_data[_pos + 1] << 8) | (_data[_pos + 2] << 16) | (_data[_pos + 3] << 24))
			  : ((_data[_pos] << 24) | (_data[_pos + 1] << 16) | (_data[_pos + 2] << 8) | _data[_pos + 3]);
			_pos += 4; return value;
		}
		public long GetLong()
		{
			Require(8);
			long value;
			if (Order == ByteOrder.LittleEndian)
			{
				value = 0;
				for (int i = 7; i >= 0; i--) value = (value << 8) | _data[_pos + i];
			}
			else
			{
				value = 0; for (int i = 0; i < 8; i++) value = (value << 8) | _data[_pos + i];
			}
			_pos += 8; return value;
		}
		public float GetFloat() { var i = GetInt(); return BitConverter.Int32BitsToSingle(i); }
		public double GetDouble() { var l = GetLong(); return BitConverter.Int64BitsToDouble(l); }
	}
}
