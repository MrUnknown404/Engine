using Engine3.Client.Graphics.Vulkan.Objects;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Renderers {
	// TODO this doesn't work well if i ever want to edit a renderpass (pipeline. fixed easily?) or use multiple pipelines (design change)
	public class VulkanRenderPassRenderer : VulkanRendererBase { // should be eventually be "bindless". we'll see how that goes...
		private readonly List<VulkanRenderPass> renderPasses = new();
		private readonly List<VulkanRenderPass> renderPassesWithUpdates = new();

		public bool CreateInitialViewport { get; set; } = true;
		public bool CreateInitialScissor { get; set; } = true;

		public VulkanRenderPassRenderer(VulkanGraphicsBackend graphicsBackend, VulkanWindow window, bool createDepthImage) : base(graphicsBackend, window, createDepthImage) { }

		public void AddRenderPass(VulkanRenderPass renderPass) {
			renderPasses.Add(renderPass);
			if (renderPass.ShouldUpdate) { renderPassesWithUpdates.Add(renderPass); }
		}

		public void SortRenderPasses(IComparer<VulkanRenderPass> comparer) {
			renderPasses.Sort(comparer);
			renderPassesWithUpdates.Sort(comparer);
		}

		protected override void CopyBuffers(float delta) {
			foreach (VulkanRenderPass renderPass in renderPasses) { renderPass.CopyBuffers(delta, FrameIndex); }
		}

		protected override void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer) {
			if (CreateInitialViewport) { commandBuffer.CmdSetViewport(0, 0, SwapChain.Extent.width, SwapChain.Extent.height, 0, 1); }
			if (CreateInitialScissor) { commandBuffer.CmdSetScissor(0, 0, SwapChain.Extent); }

			foreach (VulkanRenderPass renderPass in renderPasses.Where(static r => r.ShouldRender)) {
				commandBuffer.CmdBindGraphicsPipeline(renderPass.GraphicsPipeline.Pipeline);

				if (renderPass.VertexBuffer != null) { commandBuffer.CmdBindVertexBuffer(renderPass.VertexBuffer, renderPass.VertexFirstBinding, renderPass.VertexOffset); }
				if (renderPass.IndexBuffer != null) { commandBuffer.CmdBindIndexBuffer(renderPass.IndexBuffer, renderPass.IndexBuffer.BufferSize, VkIndexType.IndexTypeUint32, renderPass.IndexOffset); }

				renderPass.RecordCommandBuffer(commandBuffer, FrameIndex);
			}
		}

		protected override void OnSwapchainInvalid() {
			base.OnSwapchainInvalid();
			foreach (VulkanRenderPass renderPass in renderPasses) { renderPass.OnSwapchainInvalid(SwapChain); }
		}

		protected internal override void Update() {
			foreach (VulkanRenderPass renderPass in renderPassesWithUpdates) { renderPass.Update(); }
		}
	}
}