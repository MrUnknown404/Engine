using NLog;
using USharpLibs.Common.Utils;

namespace Engine3.Client.Graphics.OpenGL;

public class OpenGLSettings {
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public uint[] DisabledCallbackIds { get; init; } = Array.Empty<uint>();
	public int SwapInterval { get; init; }

	internal void Print() {
		Logger.Trace("OpenGL Backend Settings");
		Logger.Trace($"- {nameof(DisabledCallbackIds)}: {DisabledCallbackIds.ElementsAsString()}");
		Logger.Trace($"- {nameof(SwapInterval)}: {SwapInterval}");
	}
}