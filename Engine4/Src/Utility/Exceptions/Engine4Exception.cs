namespace Engine4.Utility.Exceptions;

public class Engine4Exception : Exception {
	public Engine4Exception() { }
	public Engine4Exception(string message) : base(message) { }
	public Engine4Exception(string message, Exception innerException) : base(message, innerException) { }
	public Engine4Exception(Exception innerException) : base(null, innerException) { }
}