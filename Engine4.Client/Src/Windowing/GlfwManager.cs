using System.Diagnostics.CodeAnalysis;
using Engine4.IO;
using Engine4.Utility.Exceptions;
using NLog;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OperatingSystem = Engine4.Utility.Compatability.OperatingSystem;

namespace Engine4.Client.Windowing;

public sealed class GlfwManager {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Engine);

	[SuppressMessage("ReSharper", "HeuristicUnreachableCode")]
	internal GlfwManager() {
#pragma warning disable CS0162 // Unreachable code detected
		switch (Engine4.OperatingSystem) {
			case OperatingSystem.Linux: GLFW.InitHint(InitHintPlatform.Platform, Platform.Wayland); break;
			case OperatingSystem.Windows: break; // nothing atm
			default: throw new ArgumentOutOfRangeException();
		}
#pragma warning restore CS0162 // Unreachable code detected

		GLFW.SetErrorCallback(GlfwErrorCallback);
		GLFW.Init();

		// prefer wayland over x11. Renderdoc uses x11 (pain).
		// if GLFW doesn't have any instance extensions then try again with x11.
		// this is basically done to check if we're using Renderdoc.
		// if there's a better way of doing this lemme know.
		if (GLFW.GetRequiredInstanceExtensions().Length == 0) {
			Logger.Warn("Failed to find GLFW instance extensions. Attempting to try again with X11.");
			GLFW.Terminate();

			GLFW.InitHint(InitHintPlatform.Platform, Platform.X11);
			GLFW.Init();

			if (GLFW.GetRequiredInstanceExtensions().Length == 0) { throw new Engine4Exception("Failed to setup GLFW"); }
		}

		Logger.Debug($"GLFW successfully set up. Version: {GLFW.GetVersionString()}, Platform: {GLFW.GetPlatform()}");
	}

	internal void Cleanup() {
		GLFW.Terminate();
		GLFW.SetErrorCallback(null);
	}

	private static readonly Logger GlfwLogger = LoggerH.GetLogger(LogSource.Glfw);
	private static void GlfwErrorCallback(ErrorCode error, string description) => GlfwLogger.Error($"[{error}] {description}");
}