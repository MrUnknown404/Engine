using System.Runtime.InteropServices;
using Engine3.Client.Graphics;
using NLog;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;

namespace Engine3.Client {
	public class GlWindow : Window {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public OpenGLContextHandle GLContextHandle { get; }

		public GlWindow(OpenGLGraphicsBackend graphicsBackend, string title, uint width, uint height) : base(graphicsBackend, title, width, height) {
			Logger.Debug("Creating and setting OpenGL context...");
			GLContextHandle = Toolkit.OpenGL.CreateFromWindow(WindowHandle);
			MakeContextCurrent();
			Logger.Debug($"- Version: {GL.GetString(StringName.Version)}");

#if DEBUG
			Logger.Debug("- Debug callbacks enabled");
			CreateDebugMessageCallback(graphicsBackend.DisabledCallbackIds);
#endif
		}

		public void MakeContextCurrent() {
			Toolkit.OpenGL.SetCurrentContext(GLContextHandle);
			GLLoader.LoadBindings(Toolkit.OpenGL.GetBindingsContext(GLContextHandle));
		}

		protected override void Cleanup() { }

#if DEBUG
		private static void CreateDebugMessageCallback(uint[] disabledCallbackIds) {
			GL.Enable(EnableCap.DebugOutput);
			GL.Enable(EnableCap.DebugOutputSynchronous);

			GL.DebugMessageControl(DebugSource.DebugSourceApi, DebugType.DebugTypeOther, DebugSeverity.DontCare, disabledCallbackIds.Length, disabledCallbackIds.ToArray(), false);

			GL.DebugMessageCallback(static (source, type, id, severity, length, message, _) => {
				string sourceFormatted = string.Empty;
				string typeFormatted = string.Empty;

				if (source != DebugSource.DontCare) { sourceFormatted = source.ToString()[nameof(DebugSource).Length..]; } // can source/type be <Enum>.DontCare?
				if (type != DebugType.DontCare) { typeFormatted = type.ToString()[nameof(DebugType).Length..]; }

				switch (severity) {
					case DebugSeverity.DontCare: return;
					case DebugSeverity.DebugSeverityNotification:
						Logger.Debug($"OpenGL Notification: {id}. Source: {sourceFormatted}, Type: {typeFormatted}");
						Logger.Debug($"- {Marshal.PtrToStringAnsi(message, length)}");
						break;
					case DebugSeverity.DebugSeverityHigh:
						Logger.Fatal($"OpenGL Fatal Error: {id}. Source: {sourceFormatted}, Type: {typeFormatted}");
						Logger.Fatal($"- {Marshal.PtrToStringAnsi(message, length)}");
						break;
					case DebugSeverity.DebugSeverityMedium:
						Logger.Error($"OpenGL Error: {id}. Source: {sourceFormatted}, Type: {typeFormatted}");
						Logger.Error($"- {Marshal.PtrToStringAnsi(message, length)}");
						break;
					case DebugSeverity.DebugSeverityLow:
						Logger.Warn($"OpenGL Warning: {id}. Source: {sourceFormatted}, Type: {typeFormatted}");
						Logger.Warn($"- {Marshal.PtrToStringAnsi(message, length)}");
						break;
					default: throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
				}
			}, IntPtr.Zero);
		}
#endif
	}
}