using Engine3.Api.Graphics;
using JetBrains.Annotations;
using OpenTK.Platform;

namespace Engine3.Api {
	[PublicAPI]
	public class EngineStartupSettings {
		public IRenderContext RenderContext { get; }
		public GraphicsApiHints? GraphicsApiHints { get; init; }
		public ToolkitOptions? ToolkitOptions { get; init; }
		public string WindowTitle { get; init; }
		public bool CenterWindow { get; init; }

		public EngineStartupSettings(IRenderContext renderContext, OpenGLGraphicsApiHints graphicsApiHints, ToolkitOptions toolkitOptions) {
			graphicsApiHints.Version = new(4, 6);
			graphicsApiHints.Profile = OpenGLProfile.Core;
#if DEBUG
			graphicsApiHints.DebugFlag = true;
#endif

			RenderContext = renderContext;
			GraphicsApiHints = graphicsApiHints;
			ToolkitOptions = toolkitOptions;
			WindowTitle = "Default Title - OpenGL";
		}

		public EngineStartupSettings(IRenderContext renderContext, VulkanGraphicsApiHints graphicsApiHints, ToolkitOptions toolkitOptions) {
			RenderContext = renderContext;
			GraphicsApiHints = graphicsApiHints;
			ToolkitOptions = toolkitOptions;
			WindowTitle = "Default Title - Vulkan";
		}

		public EngineStartupSettings() {
			RenderContext = null!;
			WindowTitle = "Default Title - Console";

			throw new NotImplementedException();
		}
	}
}