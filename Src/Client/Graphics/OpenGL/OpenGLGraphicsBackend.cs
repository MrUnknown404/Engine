using NLog;
using OpenTK.Platform;
using USharpLibs.Common.Utils;

namespace Engine3.Client.Graphics.OpenGL {
	public class OpenGLGraphicsBackend : EngineGraphicsBackend {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public uint[] DisabledCallbackIds { get; init; } = Array.Empty<uint>();
		public int SwapInterval { get; init; }

		public OpenGLGraphicsBackend(OpenGLGraphicsApiHints graphicsApiHints) : base(GraphicsBackend.OpenGL, graphicsApiHints) { }
		protected internal override void Setup(GameClient gameClient) { PrintSettings(); }
		protected internal override void Cleanup() { }

		private void PrintSettings() {
			Logger.Trace("OpenGL Graphics Backend Settings");
			Logger.Trace($"- {nameof(DisabledCallbackIds)}: {DisabledCallbackIds.ElementsAsString()}");
			Logger.Trace($"- {nameof(SwapInterval)}: {SwapInterval}");
		}
	}
}