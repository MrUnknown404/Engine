using Engine3.Client.Graphics.Vulkan.Objects;

namespace Engine3.Client.Graphics.Vulkan.Renderers {
	public abstract class VulkanRecorderNode {
		public bool ShouldDraw { get; set; } = true;

		protected internal abstract void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer, byte frameIndex);
		protected internal abstract void OnSwapChainChange(SwapChain newSwapChain);
		protected internal abstract void CopyBuffers(float delta, byte frameIndex);
	}
}