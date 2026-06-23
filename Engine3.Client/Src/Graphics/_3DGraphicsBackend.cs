using OpenTK.Platform;
using GraphicsApi = Engine3.Core.Utility.GraphicsApi;

namespace Engine3.Client.Graphics;

public abstract class _3DGraphicsBackend : GraphicsBackend {
	public GraphicsApiHints GraphicsApiHints { get; }

	protected _3DGraphicsBackend(VulkanGraphicsApiHints graphicsApiHints) : base(GraphicsApi.Vulkan) => GraphicsApiHints = graphicsApiHints;
	protected _3DGraphicsBackend(OpenGLGraphicsApiHints graphicsApiHints) : base(GraphicsApi.OpenGL) => GraphicsApiHints = graphicsApiHints;
}