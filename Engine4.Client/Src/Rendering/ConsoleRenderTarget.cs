using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Graphics.Vulkan.Objects;
using Engine4.Utility.Math;
using OpenTK.Graphics.Vulkan;
using USharpLibs.Common.Math;

namespace Engine4.Client.Rendering;

// vulkan to console
public class ConsoleRenderTarget : RenderTarget {
	public override BoundPhysicalGpu PhysicalGpu => throw new NotImplementedException(); // TODO impl
	public override LogicalGpu LogicalGpu => throw new NotImplementedException();

	private readonly ConsoleRenderer consoleRenderer;

	internal ConsoleRenderTarget(ConsoleRenderer consoleRenderer, Color3 clearColor) : base(clearColor) => this.consoleRenderer = consoleRenderer;

	protected internal override bool TryBeginFrame(VulkanRenderer.FrameInFlight frame) => throw new NotImplementedException(); // TODO

	protected internal override void CmdBeginRendering(GraphicsCommandBuffer graphicsCommandBuffer, DepthImage? depthImage) => throw new NotImplementedException();
	protected internal override void CmdEndRendering(GraphicsCommandBuffer graphicsCommandBuffer) => throw new NotImplementedException();

	protected internal override void PresentFrame(VulkanRenderer.FrameInFlight frame) => throw new NotImplementedException(); // TODO

	protected internal override VkSemaphore GetSignalSemaphore() => throw new NotImplementedException();

	public override Vec2<ushort> GetFrameBufferSize() => throw new NotImplementedException(); // TODO

	protected internal override void Cleanup() { }
}