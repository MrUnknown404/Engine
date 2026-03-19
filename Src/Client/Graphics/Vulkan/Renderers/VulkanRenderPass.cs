using Engine3.Client.Graphics.Vulkan.Objects;

namespace Engine3.Client.Graphics.Vulkan.Renderers {
	public abstract class VulkanRenderPass {
		protected VulkanResourceProvider GraphicsResourceProvider { get; }
		protected TransferCommandPool TransferCommandPool { get; }
		protected SurfaceCapablePhysicalGpu PhysicalGpu { get; }
		protected LogicalGpu LogicalGpu { get; }
		protected byte MaxFramesInFlight { get; }

		public string DebugName { get; }

		public GraphicsPipeline GraphicsPipeline { get; } // TODO editable graphics pipeline?

		public VulkanBuffer? VertexBuffer { get; protected set; }
		public VulkanBuffer? IndexBuffer { get; protected set; }

		public uint VertexFirstBinding { get; set; }
		public ulong VertexOffset { get; set; }
		public ulong IndexOffset { get; set; }

		public virtual bool ShouldRender { get; set; } = true;
		protected internal bool ShouldUpdate { get; protected init; }

		protected VulkanRenderPass(string debugName, VulkanRenderPassRenderer renderer, GraphicsPipeline graphicsPipeline) {
			DebugName = debugName;
			GraphicsResourceProvider = renderer.GraphicsResourceProvider;
			TransferCommandPool = renderer.TransferCommandPool;
			PhysicalGpu = renderer.PhysicalGpu;
			LogicalGpu = renderer.LogicalGpu;
			MaxFramesInFlight = renderer.MaxFramesInFlight;
			GraphicsPipeline = graphicsPipeline;
		}

		protected internal virtual void Update() { }

		protected internal abstract void CopyBuffers(float delta, byte frameIndex);
		protected internal abstract void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer, byte frameIndex);
		protected internal virtual void OnSwapchainInvalid(SwapChain swapChain) { }

		public override bool Equals(object? obj) => obj is VulkanRenderPass renderPass && renderPass.DebugName == DebugName;
		public override int GetHashCode() => DebugName.GetHashCode();
	}
}