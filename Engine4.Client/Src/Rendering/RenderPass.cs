using Engine4.Client.Graphics;

namespace Engine4.Client.Rendering;

public abstract class RenderPass { // TODO abstract?
	public IGraphicsPipeline GraphicsPipeline { get; set; }

	protected RenderPass(IGraphicsPipeline graphicsPipeline) => GraphicsPipeline = graphicsPipeline;
}