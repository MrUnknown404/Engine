using Engine4.Client.Graphics.Vulkan.Objects;

namespace Engine4.Client.Graphics._Test;

public class RenderPass2 {
	public RenderGraph.RenderPassHandle Handle { get; }
	public bool Enabled { get; set; }

	internal RenderPassStage RenderPassStage { get; }
	internal Action<GraphicsCommandBuffer>? Exec { get; } // record graphics buffer. etc

	internal RenderGraph.IResourceHandle[] Inputs { get; }
	internal RenderGraph.IResourceHandle[] Outputs { get; }

	internal List<object> ColorAttachments { get; } = new(); // TODO ?

	// TODO store barriers?

	internal RenderPass2(RenderPassBuilder builder) {
		Handle = builder.Handle;
		RenderPassStage = builder.RenderPassStage;
		Exec = builder.Exec;
		Inputs = builder.Inputs.ToArray();
		Outputs = builder.Outputs.ToArray();
	}
}