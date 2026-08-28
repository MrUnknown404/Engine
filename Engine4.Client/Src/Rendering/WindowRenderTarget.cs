using Engine4.Client.Graphics;

namespace Engine4.Client.Rendering;

public class WindowRenderTarget : RenderTarget {
	public WindowRenderTarget(GameClient game, Window window) : base(window.GraphicsApi, window.GraphicsApi switch {
			GraphicsApi.None => throw new Exception(), // TODO exception
			GraphicsApi.OpenGL => game.OpenGLGraphicsProvider ?? throw new Exception(), // TODO exception
			GraphicsApi.Vulkan => game.VulkanGraphicsProvider ?? throw new Exception(), // TODO exception
			GraphicsApi.Software => game.SoftwareGraphicsProvider ?? throw new Exception(), // TODO exception
			_ => throw new ArgumentOutOfRangeException(),
	}) { }
}