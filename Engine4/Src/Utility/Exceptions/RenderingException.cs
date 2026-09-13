namespace Engine4.Utility.Exceptions;

public class RenderingException : Engine4Exception {
	public RenderingException() { }
	public RenderingException(string message, Exception? innerException = null) : base(message, innerException) { }
	public RenderingException(Exception innerException) : base(innerException) { }
}