namespace Engine4.Utility.Exceptions;

public class IllegalStateException : Exception {
	public IllegalStateException() { }
	public IllegalStateException(string message) : base(message) { }
	public IllegalStateException(string message, Exception exception) : base(message, exception) { }
	public IllegalStateException(Exception exception) : base(null, exception) { }
}