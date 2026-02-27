using Engine3.Client.Graphics.Vulkan.Objects;

namespace Engine3.Client.Graphics.Vulkan.Renderers {
	public class VulkanNodeRenderer : VulkanRendererBase {
		private readonly List<VulkanRecorderNode> nodes = new();

		protected VulkanNodeRenderer(VulkanGraphicsBackend graphicsBackend, VulkanWindow window) : base(graphicsBackend, window) { }

		protected void AddNode(VulkanRecorderNode node) => nodes.Add(node);

		protected override void CopyBuffers(float delta) {
			foreach (VulkanRecorderNode node in nodes) { node.CopyBuffers(delta, FrameIndex); }
		}

		protected override void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer) {
			foreach (VulkanRecorderNode node in nodes) { node.RecordCommandBuffer(commandBuffer, FrameIndex); }
		}

		protected override void OnSwapchainInvalid() {
			base.OnSwapchainInvalid();
			foreach (VulkanRecorderNode node in nodes) { node.OnSwapChainChange(SwapChain); }
		}
	}
}