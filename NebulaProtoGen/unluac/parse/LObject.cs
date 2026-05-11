namespace unluac.parse
{
	public abstract class LObject : BObject
	{
		public virtual string Deref() => throw new InvalidOperationException();
		public abstract override bool Equals(object? o);
		public override int GetHashCode() => base.GetHashCode();
	}
}
