using Engine3.Api.Graphics;
using OpenTK.Platform;

namespace Engine3.Api {
	public interface IEngineStartupSettings {
		public IRenderContext RenderContext { get; }
		public GraphicsApiHints? GraphicsApiHints { get; init; }
		public ToolkitOptions? ToolkitOptions { get; init; }
		public string WindowTitle { get; init; }
		public bool CenterWindow { get; init; }
	}
}