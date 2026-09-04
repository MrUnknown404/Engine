using NLog;
using NLog.Time;

namespace Engine4.IO;

public class LoggingSettings {
	public bool ShowTime { get; init; } = true;
	public bool ShowLogLevel { get; init; } = true;
	public bool ShowThread { get; init; }
	public bool ShowSource { get; init; } = true;
	public bool ShowCallsite { get; init; } = true;

	public string? CustomLayout { get; init; }

	public string LogFileDateFormat { get; init; } = "MM-dd-yyyy HH-mm-ss-fff";
	public string LogFileDirectory { get; init; } = "Logs";
	public string LogFileExtension { get; init; } = "log";

	public byte MaxLogFiles { get; init; } = 5;

	public bool PrintToConsole { get; init; } = true;
	public bool PrintToFile { get; init; } = true;

	public LogLevel ConsoleLogLevel { get; init; } =
#if DEBUG
		LogLevel.Trace;
#else
		LogLevel.Info;
#endif

	public LogLevel FileLogLevel { get; init; } =
#if DEBUG
		LogLevel.Trace;
#else
		LogLevel.Debug;
#endif

	public TimeSource? TimeSource { get; init; }
}