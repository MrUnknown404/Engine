using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public class VulkanTexture : VulkanResource { // TODO
	public override ulong Handle => Image.Handle;

	internal VkImage Image { get; }
	internal VkDeviceMemory Memory { get; }
	internal VkImageView View { get; }

	internal VulkanTexture(string debugName) : base(debugName) { }

	protected internal override void Cleanup() => throw new NotImplementedException();
}