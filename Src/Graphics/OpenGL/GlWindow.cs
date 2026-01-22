using System.Runtime.InteropServices;
using NLog;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;

namespace Engine3.Graphics.OpenGL {
	public class GlWindow : Window {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public OpenGLContextHandle GLContextHandle { get; }
		public VertexArrayHandle EmptyVao { get; }

		public ClearBufferMask ClearBufferMask { get; set; } = ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit;

		private GlWindow(WindowHandle windowHandle, OpenGLContextHandle glContextHandle, VertexArrayHandle emptyVao) : base(windowHandle) {
			GLContextHandle = glContextHandle;
			EmptyVao = emptyVao;
		}

		internal static GlWindow MakeGlWindow(GameClient gameClient, WindowHandle windowHandle) {
			Logger.Debug("Creating and setting OpenGL context...");
			OpenGLContextHandle openGLContextHandle = Toolkit.OpenGL.CreateFromWindow(windowHandle);
			Toolkit.OpenGL.SetCurrentContext(openGLContextHandle);
			GLLoader.LoadBindings(Toolkit.OpenGL.GetBindingsContext(openGLContextHandle)); // do i call this per window?
			Logger.Debug($"- Version: {GL.GetString(StringName.Version)}");

#if DEBUG
			Logger.Debug("- Debug callbacks enabled");
			CreateDebugMessageCallback(gameClient.DisabledCallbackIds);
#endif

			VertexArrayHandle emptyVao = new(GL.CreateVertexArray());
			GL.BindVertexArray((int)emptyVao); // Some hardware requires vao to be bound even if it's not in use
			Logger.Debug($"EmptyVao has Handle: {emptyVao.Handle}");

			GlWindow window = new(windowHandle, openGLContextHandle, emptyVao);

			GL.ClearColor(window.ClearColor);
			GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.CullFace);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

			return window;
		}

		protected override void CleanupGraphics() { }

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