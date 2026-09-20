using Engine4.Client.Graphics.Vulkan.Objects;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics._Test;

[MustDisposeResource]
public sealed class RenderPassBuilder : IDisposable {
	internal RenderGraph.RenderPassHandle Handle { get; }
	internal RenderPassStage RenderPassStage { get; }
	internal Action<GraphicsCommandBuffer>? Exec { get; } // record graphics buffer. etc?

	internal bool Invalid { get; private set; }

	internal List<RenderGraph.IResourceHandle> Inputs { get; } = new();
	internal List<RenderGraph.IResourceHandle> Outputs { get; } = new();

	public RenderPassBuilder(string renderPassName, RenderPassStage renderPassStage, Action<GraphicsCommandBuffer>? exec) {
		Handle = new(renderPassName);
		RenderPassStage = renderPassStage;
		Exec = exec;
	}

	// how do i want to do this?
	public void AddInput(RenderGraph.BufferHandle resource) => Inputs.Add(resource);
	public void AddInput(RenderGraph.ImageHandle resource, VkImageLayout imageLayout) => Inputs.Add(resource); // TODO other parameters? if not merge these methods
	public void AddOutput(RenderGraph.BufferHandle resource) => Outputs.Add(resource);
	public void AddOutput(RenderGraph.ImageHandle resource) => Outputs.Add(resource); // TODO other parameters? if not merge these methods

	public void AddColorAttachment(string resourceName, VkClearValue clearValue) => throw new NotImplementedException(); // TODO
	public void AddDepthAttachment(string resourceName, VkClearValue clearValue) => throw new NotImplementedException(); // TODO

	public void BindBuffer(RenderGraph.BufferHandle resource, byte binding) => throw new NotImplementedException(); // TODO
	public void UseShader(string resourceName) => throw new NotImplementedException(); // TODO
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