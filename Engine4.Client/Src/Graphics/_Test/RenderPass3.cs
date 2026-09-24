using Engine4.Client.Graphics.Vulkan.Objects;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics._Test;

// TODO arrays for readonly version
public abstract class RenderPass3 {
	public bool Enabled { get; protected internal set; } = true;

	protected internal List<RenderGraph3.ReadData> Inputs { get; } = new();
	protected internal List<RenderGraph3.WriteData> Outputs { get; } = new();

	// set on compile
	protected internal VkBufferMemoryBarrier2[] BufferMemoryBarriers { get; internal set; } = Array.Empty<VkBufferMemoryBarrier2>();
	protected internal VkImageMemoryBarrier2[] ImageMemoryBarriers { get; internal set; } = Array.Empty<VkImageMemoryBarrier2>();

	protected internal RenderGraph3 RenderGraph { get; internal set; } = null!; // set in RenderGraph.AddPass

	protected internal abstract void Execute(GraphicsCommandBuffer commandBuffer);

	// ADD
	public void AddInput(RenderGraph3.BufferHandle handle, VkPipelineStageFlagBits2 stageMask) => Inputs.Add(new RenderGraph3.BufferReadData(handle, VkAccessFlagBits2.Access2ShaderReadBit, stageMask));

	public void AddInput(RenderGraph3.TextureHandle handle, VkPipelineStageFlagBits2 stageMask, VkImageLayout imageLayout) =>
			Inputs.Add(new RenderGraph3.ImageReadData(handle, VkAccessFlagBits2.Access2ShaderReadBit, stageMask, imageLayout));

	public void AddOutput(RenderGraph3.BufferHandle handle) => Outputs.Add(new(handle));
	public void AddOutput(RenderGraph3.TextureHandle handle) => Outputs.Add(new(handle));
}