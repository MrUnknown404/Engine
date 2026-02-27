using Engine3.Client.Graphics.Vulkan.Objects;

namespace Engine3.Client.Graphics.Vulkan.Renderers {
	public abstract class VulkanRenderPass {
		public GraphicsPipeline GraphicsPipeline { get; }

		public VulkanBuffer? VertexBuffer { get; protected set; }
		public VulkanBuffer? IndexBuffer { get; protected set; }

		public uint VertexFirstBinding { get; set; }
		public ulong VertexOffset { get; set; }
		public ulong IndexOffset { get; set; }

		public virtual bool ShouldRender { get; set; } = true;
		protected internal bool ShouldUpdate { get; protected init; }

		protected LogicalGpu LogicalGpu { get; }

		protected VulkanRenderPass(LogicalGpu logicalGpu, GraphicsPipeline graphicsPipeline) {
			LogicalGpu = logicalGpu;
			GraphicsPipeline = graphicsPipeline;
		}

		protected internal virtual void Update() { }

		protected internal abstract void CopyBuffers(float delta, byte frameIndex);
		protected internal abstract void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer, byte frameIndex);
		protected internal virtual void OnSwapchainInvalid(SwapChain swapChain) { }
	}
}