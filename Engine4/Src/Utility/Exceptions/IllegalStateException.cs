using JetBrains.Annotations;

namespace Engine4.Utility.Exceptions;

[PublicAPI]
public class IllegalStateException : Exception {
	public IllegalStateException() { }
	public IllegalStateException(string message, Exception? exception = null) : base(message, exception) { }
	public IllegalStateException(Exception exception) : base(null, exception) { }
}