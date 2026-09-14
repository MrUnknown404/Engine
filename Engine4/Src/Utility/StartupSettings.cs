using Engine4.IO;
using NLog;

namespace Engine4.Utility;

public abstract class StartupSettings {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Engine);

	public string MainThreadName { get; init; } = "Main";
	public LoggingSettings LoggingSettings { get; init; } = new();

	protected internal virtual void PrintValues() {
		Logger.Debug($"- {nameof(MainThreadName)}: {MainThreadName}");
		LoggingSettings.PrintValue();
	}
}