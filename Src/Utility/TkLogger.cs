using NLog;
using ILogger = OpenTK.Core.Utility.ILogger;
using LogLevel = OpenTK.Core.Utility.LogLevel;

namespace Engine3.Utility {
	public class TkLogger : ILogger {
		public static LogLevel FilterStatic { get; set; } = LogLevel.Warning;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public LogLevel Filter { get => FilterStatic; set => FilterStatic = value; }

		public void LogInternal(string str, LogLevel level, string filePath, int lineNumber, string member) {
			if (level < Filter) { return; }

			switch (level) {
				case LogLevel.Debug: Logger.Debug($"[TkLogger] {str}"); break;
				case LogLevel.Info: Logger.Info($"[TkLogger] {str}"); break;
				case LogLevel.Warning: Logger.Warn($"[TkLogger] {str}"); break;
				case LogLevel.Error: Logger.Error($"[TkLogger] {str}"); break;
				default: throw new ArgumentOutOfRangeException(nameof(level), level, null);
			}
		}

		public void Flush() { }
	}
}