using Engine3.Client.Graphics.Vulkan.Objects;

namespace Engine3.Client.Graphics.Vulkan {
	public abstract class VulkanNodeRenderer : VulkanRenderer {
		private readonly List<VulkanRecorderNodeEnd> nodes = new();

		protected VulkanNodeRenderer(VulkanGraphicsBackend graphicsBackend, VulkanWindow window) : base(graphicsBackend, window) { }

		protected void AddNode(VulkanRecorderNodeEnd node) => nodes.Add(node);

		protected override void CopyBuffers(float delta) {
			foreach (VulkanRecorderNodeEnd node in nodes) { node.CopyBuffers(delta, FrameIndex); }
		}

		protected override void RecordCommandBuffer(GraphicsCommandBuffer graphicsCommandBuffer) {
			foreach (VulkanRecorderNodeEnd node in nodes) { node.RecordCommandBuffer(graphicsCommandBuffer, FrameIndex); }
		}

		protected override void OnSwapchainInvalid() {
			base.OnSwapchainInvalid();
			foreach (VulkanRecorderNodeEnd node in nodes) { node.OnSwapChainChange(SwapChain); }
		}
	}
}