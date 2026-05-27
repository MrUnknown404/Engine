using Engine3.Client.Client.Graphics.Vulkan.Objects;

namespace Engine3.Client.Client.Graphics.Vulkan.Renderers;

public abstract class VulkanRecorderNode {
	protected VulkanResourceProvider GraphicsResourceProvider { get; }
	protected TransferCommandPool TransferCommandPool { get; }
	protected SurfaceCapablePhysicalGpu PhysicalGpu { get; }
	protected LogicalGpu LogicalGpu { get; }
	protected byte MaxFramesInFlight { get; }

	public bool ShouldDraw { get; set; } = true;

	protected VulkanRecorderNode(VulkanNodeRenderer renderer) {
		GraphicsResourceProvider = renderer.GraphicsResourceProvider;
		TransferCommandPool = renderer.TransferCommandPool;
		PhysicalGpu = renderer.PhysicalGpu;
		LogicalGpu = renderer.LogicalGpu;
		MaxFramesInFlight = renderer.MaxFramesInFlight;
	}

	protected internal abstract void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer, byte frameIndex);
	protected internal abstract void OnSwapChainChange(SwapChain newSwapChain);
	protected internal abstract void CopyBuffers(float delta, byte frameIndex);
}