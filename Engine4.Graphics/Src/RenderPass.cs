namespace Engine4.Graphics;

public class RenderPass {
	public IGraphicsPipeline GraphicsPipeline { get; set; }

	public RenderPass(IGraphicsPipeline graphicsPipeline) => GraphicsPipeline = graphicsPipeline;
}