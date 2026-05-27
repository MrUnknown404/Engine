namespace Engine3.Core.Utility.Exceptions;

public interface IEnumException<T> {
	public T ReasonValue { get; }

	public static abstract string ReasonToString(T reason);
}