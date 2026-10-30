namespace Engine4.Client.Graphics.Vulkan.Objects;

public abstract class NamedVulkanResource : VulkanResource {
	public string DebugName { get; }

	protected NamedVulkanResource(string debugName) => DebugName = debugName;
}