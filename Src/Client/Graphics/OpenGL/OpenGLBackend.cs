using OpenTK.Platform;

namespace Engine3.Client.Graphics.OpenGL;

public class OpenGLBackend : EngineGraphicsBackend {
	public OpenGLSettings Settings { get; init; } = new();

	public OpenGLBackend(OpenGLGraphicsApiHints graphicsApiHints) : base(GraphicsBackend.OpenGL, graphicsApiHints) { }

	protected internal override void Setup(GameClient gameClient) => Settings.Print();
	protected internal override void Cleanup() { }
}