using Engine3.Core;
using OpenTK.Platform;

namespace Engine3.Client.Graphics.OpenGL;

public class OpenGLBackend : _3DGraphicsBackend {
	public OpenGLSettings Settings { get; init; } = new();

	public OpenGLBackend(OpenGLGraphicsApiHints graphicsApiHints) : base(graphicsApiHints) { }

	protected internal override void Setup(EngineGame game) => Settings.Print();
	protected internal override void Cleanup() { }
}