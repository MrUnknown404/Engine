namespace Engine4.Client.Graphics.Vulkan.Objects;

public class VulkanTexture : VulkanResource { // TODO
	protected override ulong Handle => throw new NotImplementedException();

	public VulkanTexture(string debugName) : base(debugName) { }

	protected internal override void Cleanup() => throw new NotImplementedException();
}