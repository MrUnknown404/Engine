using Engine3.Client.Client.Graphics.Vulkan.Objects;

namespace Engine3.Client.Client.Graphics.Vulkan.Renderers;

public abstract class VulkanIndirectRenderPass : VulkanRenderPass {
	public VulkanBuffer? IndirectCmdBuffer { get; protected set; }

	protected VulkanIndirectRenderPass(string debugName, VulkanRenderPassRenderer renderer, GraphicsPipeline graphicsPipeline) : base(debugName, renderer, graphicsPipeline) { }
}