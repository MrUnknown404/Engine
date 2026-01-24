namespace Engine3.Exceptions {
	public interface IEnumException<T> {
		public T ReasonEnum { get; }

		public static abstract string ReasonToString(T reason);
	}
}