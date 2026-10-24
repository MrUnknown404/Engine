using JetBrains.Annotations;

namespace Engine4.Client.Utility.Exceptions;

[PublicAPI]
public class ShadercException : Exception {
	public ShadercException() { }
	public ShadercException(string message, Exception? innerException = null) : base(message, innerException) { }
	public ShadercException(Exception innerException) : base(null, innerException) { }
}