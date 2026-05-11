namespace unluac.parse
{
	public class LUpvalue : BObject
	{
		public bool instack; public int idx; public string? name;
		public override bool Equals(object? obj)
		{
			if (obj is LUpvalue u)
			{
				if (!(instack == u.instack && idx == u.idx)) return false;
				if (ReferenceEquals(name, u.name)) return true;
				return name != null && name.Equals(u.name);
			}
			return false;
		}
	}
}
