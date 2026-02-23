using Engine3.Client.Graphics.Vulkan.Objects;

namespace Engine3.Client.Graphics.Vulkan {
	public abstract class VulkanRecorderNodeEnd {
		public bool ShouldDraw { get; set; } = true;

		protected internal abstract void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer, byte frameIndex);
		protected internal abstract void OnSwapChainChange(SwapChain newSwapChain);
		protected internal abstract void CopyBuffers(float delta, byte frameIndex);
	}

	public abstract class VulkanRecorderNode : VulkanRecorderNodeEnd {
		private readonly List<VulkanRecorderNode> children = new();

		public void AddChild(VulkanRecorderNode child) => children.Add(child);

		protected internal override void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer, byte frameIndex) {
			foreach (VulkanRecorderNode child in children) { child.RecordCommandBuffer(commandBuffer, frameIndex); }
		}

		protected internal override void OnSwapChainChange(SwapChain newSwapChain) {
			foreach (VulkanRecorderNode child in children) { child.OnSwapChainChange(newSwapChain); }
		}

		protected internal override void CopyBuffers(float delta, byte frameIndex) {
			foreach (VulkanRecorderNode child in children) { child.CopyBuffers(delta, frameIndex); }
		}
	}
}