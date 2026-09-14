using Engine4.IO;
using Engine4.Utility.Versions;
using JetBrains.Annotations;
using NLog;
using OperatingSystem = Engine4.Utility.Compatability.OperatingSystem;

namespace Engine4;

[PublicAPI]
public static class Engine4 {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Engine);

	public const string Name = nameof(Engine4);
	public const bool Debug =
#if DEBUG
			true;
#else
			false;
#endif
	public const OperatingSystem OperatingSystem =
#if OS_LINUX
			Utility.Compatability.OperatingSystem.Linux;
#elif OS_WINDOWS
			Utility.Compatability.OperatingSystem.Windows;
#endif

	public static readonly IPackableVersion Version = new BuildVersion(0);

	internal static void PrintStartup() {
		Logger.Debug($"- Engine Name: {Name}");
		Logger.Debug($"- Version: {Version}");
		Logger.Debug($"- Debug: {Debug}");
	}
}