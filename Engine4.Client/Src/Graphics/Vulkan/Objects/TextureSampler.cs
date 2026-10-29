using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public class TextureSampler : VulkanResource { // TODO
	public override ulong Handle => Sampler.Handle;

	internal VkSampler Sampler { get; }

	internal TextureSampler(string debugName) : base(debugName) { }

	protected internal override void Cleanup() => throw new NotImplementedException();
}