using Engine4.IO;
using Engine4.Utility.Versions;
using NLog;

namespace Engine4;

public static class Engine4 {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Engine);

	public const string Name = nameof(Engine4);
	public const bool Debug =
#if DEBUG
			true;
#else
			false;
#endif

	public static readonly IPackableVersion Version = new BuildVersion(0);

	internal static void PrintStartup() {
		Logger.Debug($"- Engine Name: {Name}");
		Logger.Debug($"- Version: {Version}");
		Logger.Debug($"- Debug: {Debug}");
	}
}