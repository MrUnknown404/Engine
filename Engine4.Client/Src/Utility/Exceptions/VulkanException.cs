using JetBrains.Annotations;

namespace Engine4.Client.Utility.Exceptions;

[PublicAPI]
public class VulkanException : Exception {
	public VulkanException() { }
	public VulkanException(string message, Exception? innerException = null) : base(message, innerException) { }
	public VulkanException(Exception innerException) : base(null, innerException) { }
}