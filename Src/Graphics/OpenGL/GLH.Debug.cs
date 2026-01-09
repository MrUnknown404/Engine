#if DEBUG

using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace Engine3.Graphics.OpenGL {
	public static partial class GLH {
		static GLH() {
			DisabledCallbackIds.Add(131185); // Nvidia static buffer notification
		}

		public static void CreateDebugMessageCallback() {
			GL.Enable(EnableCap.DebugOutput);
			GL.Enable(EnableCap.DebugOutputSynchronous);

			GL.DebugMessageControl(DebugSource.DebugSourceApi, DebugType.DebugTypeOther, DebugSeverity.DontCare, DisabledCallbackIds.Count, DisabledCallbackIds.ToArray(), false);

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
	}
}

#endif