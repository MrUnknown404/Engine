namespace Engine4.Graphics.Rendering;

public abstract class RenderPass { // TODO abstract?
	public IGraphicsPipeline GraphicsPipeline { get; set; }

	protected RenderPass(IGraphicsPipeline graphicsPipeline) => GraphicsPipeline = graphicsPipeline;
}