using Engine4.Client.Graphics.Vulkan.Objects;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics._Test;

[MustDisposeResource]
public sealed class RenderPassBuilder : IDisposable {
	internal RenderGraph.RenderPassHandle Handle { get; }
	internal RenderPassStage RenderPassStage { get; }
	internal Action<GraphicsCommandBuffer>? Exec { get; } // record graphics buffer. etc

	internal bool Invalid { get; private set; }

	internal Dictionary<RenderGraph.IResourceHandle, List<RenderGraph.RenderPassHandle>> Inputs { get; } = new();
	internal Dictionary<RenderGraph.IResourceHandle, List<RenderGraph.RenderPassHandle>> Outputs { get; } = new();

	public RenderPassBuilder(string renderPassName, RenderPassStage renderPassStage, Action<GraphicsCommandBuffer>? exec) {
		Handle = new(renderPassName);
		RenderPassStage = renderPassStage;
		Exec = exec;
	}

	public void AddInput(RenderGraph.BufferHandle resource) => Get(Inputs, resource).Add(Handle);
	public void AddInput(RenderGraph.ImageHandle resource) => Get(Inputs, resource).Add(Handle); // TODO other parameters? if not merge these methods
	public void AddOutput(RenderGraph.BufferHandle resource) => Get(Outputs, resource).Add(Handle);
	public void AddOutput(RenderGraph.ImageHandle resource) => Get(Outputs, resource).Add(Handle); // TODO other parameters? if not merge these methods

	public void AddColorAttachment(string resourceName, VkClearValue clearValue) => throw new NotImplementedException(); // TODO
	public void AddDepthStencilAttachment(string resourceName, VkClearValue clearValue) => throw new NotImplementedException(); // TODO
	// more?

	private static List<RenderGraph.RenderPassHandle> Get<T>(Dictionary<RenderGraph.IResourceHandle, List<RenderGraph.RenderPassHandle>> list, T resource) where T : RenderGraph.IResourceHandle {
		if (!list.TryGetValue(resource, out List<RenderGraph.RenderPassHandle>? renderPassHandles)) {
			renderPassHandles = new();
			list[resource] = renderPassHandles;
		}

		return renderPassHandles;
	}

	public void Dispose() {
		if (Invalid) { return; }
		Invalid = true;
	}
}