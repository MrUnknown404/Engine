using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Graphics.Vulkan.Objects;
using Engine4.Utility.Math;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;
using USharpLibs.Common.Math;

namespace Engine4.Client.Rendering;

public abstract class RenderTarget { // for vulkan
	public abstract BoundPhysicalGpu PhysicalGpu { get; }
	public abstract LogicalGpu LogicalGpu { get; }

	public bool IsFrameBufferDirty { get; protected set; } // TODO set on resize

	protected RenderTarget(Color3 clearColor) => ClearColor = clearColor;

	public Color3 ClearColor { get; }

	[MustUseReturnValue]
	protected internal abstract bool TryBeginFrame(VulkanRenderer.FrameInFlight frame);

	protected internal abstract void CmdBeginRendering(GraphicsCommandBuffer graphicsCommandBuffer, DepthImage? depthImage);
	protected internal abstract void CmdEndRendering(GraphicsCommandBuffer graphicsCommandBuffer);

	protected internal abstract void PresentFrame(VulkanRenderer.FrameInFlight frame);

	protected internal abstract VkSemaphore GetSignalSemaphore();

	[MustUseReturnValue]
	public abstract Vec2<ushort> GetFrameBufferSize();

	protected internal abstract void Cleanup();
}