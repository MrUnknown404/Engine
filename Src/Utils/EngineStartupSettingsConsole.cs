using Engine3.Api;
using Engine3.Api.Graphics;
using OpenTK.Platform;

namespace Engine3.Utils {
	public class EngineStartupSettingsConsole : IEngineStartupSettings { // TODO impl
		public IRenderContext RenderContext => throw new NotImplementedException();
		public GraphicsApiHints? GraphicsApiHints { get; init; }
		public ToolkitOptions? ToolkitOptions { get; init; }
		public string WindowTitle { get; init; } = string.Empty;
		public bool CenterWindow { get; init; }
	}
}