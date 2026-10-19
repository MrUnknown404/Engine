using Engine4.Client.Graphics.Vulkan.Objects;

namespace Engine4.Client.Rendering;

public abstract class RenderPass { // TODO abstract?
	public GraphicsPipeline GraphicsPipeline { get; set; }

	protected RenderPass(GraphicsPipeline graphicsPipeline) => GraphicsPipeline = graphicsPipeline;

	protected internal abstract void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer);
}