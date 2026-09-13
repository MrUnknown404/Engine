namespace Engine4.Utility.Exceptions;

public class VulkanException : Engine4Exception {
	public VulkanException() { }
	public VulkanException(string message, Exception? innerException = null) : base(message, innerException) { }
	public VulkanException(Exception innerException) : base(innerException) { }
}