using Engine3.Client.Graphics.Vulkan.Objects;

namespace Engine3.Client.Graphics.Vulkan.Renderers {
	public abstract class VulkanRecorderTreeNode : VulkanRecorderNode {
		private readonly List<VulkanRecorderTreeNode> children = new();

		public void AddChild(VulkanRecorderTreeNode child) => children.Add(child);

		protected internal override void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer, byte frameIndex) {
			foreach (VulkanRecorderTreeNode child in children) { child.RecordCommandBuffer(commandBuffer, frameIndex); }
		}

		protected internal override void OnSwapChainChange(SwapChain newSwapChain) {
			foreach (VulkanRecorderTreeNode child in children) { child.OnSwapChainChange(newSwapChain); }
		}

		protected internal override void CopyBuffers(float delta, byte frameIndex) {
			foreach (VulkanRecorderTreeNode child in children) { child.CopyBuffers(delta, frameIndex); }
		}
	}
}