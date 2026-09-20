using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public class VulkanTexture : VulkanResource { // TODO
	internal VkImage Image { get; }
	internal VkDeviceMemory Memory { get; }
	internal VkImageView View { get; }

	protected override ulong Handle => Image.Handle;

	public VulkanTexture(string debugName) : base(debugName) { }

	protected internal override void Cleanup() => throw new NotImplementedException();
}