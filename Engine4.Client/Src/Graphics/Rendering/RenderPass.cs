using Engine4.Client.Graphics.Vulkan.Objects;

namespace Engine4.Client.Graphics.Rendering;

public abstract class RenderPass { // TODO abstract?
	protected abstract GraphicsPipeline GraphicsPipeline { get; }

	protected internal abstract void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer);
}