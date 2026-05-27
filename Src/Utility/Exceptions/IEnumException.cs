namespace Engine3.Utility.Exceptions;

public interface IEnumException<T> {
	public T ReasonEnum { get; }

	public static abstract string ReasonToString(T reason);
}