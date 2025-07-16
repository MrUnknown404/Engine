using Engine3.Graphics;
using JetBrains.Annotations;
using OpenTK.Platform;

namespace Engine3.Utils {
	[PublicAPI]
	public sealed class EngineStartupSettings {
		public RenderSystem RenderSystem { get; init; } = RenderSystem.OpenGL;
		public GraphicsApiHints? GraphicsApiHints { get; init; } = new OpenGLGraphicsApiHints {
				Version = new(4, 6),
				Profile = OpenGLProfile.Core,
#if DEBUG
				DebugFlag = true,
#endif
		};
		public ToolkitOptions ToolkitOptions { get; init; } = new();
	}
}