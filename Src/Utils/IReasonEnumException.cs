namespace Engine3.Utils {
	public interface IReasonEnumException<out T> where T : unmanaged, Enum {
		public T ReasonRef { get; }
	}
}