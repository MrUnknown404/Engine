using System.Globalization;
using NLog;

namespace Engine3.Utils {
	public static class LoggerH {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public const string FileType = "log";

		public static ushort MaxLogFiles { get; set; } = 5;
		public static string LogFolder { get; set; } = "Logs";
		/// <summary> See https://nlog-project.org/config/?tab=layout-renderers </summary>
		public static string LogLayout { get; set; } = "[${processtime}] [${level}] [${callsite:includeNamespace=False}#${callsite-linenumber}] ${message}";

		public static LogLevel ConsoleLogLevel { get; set; } = LogLevel.Debug;
		public static LogLevel FileLogLevel { get; set; } = LogLevel.Debug;

		internal static void Setup() {
			const string LogDateFormat = "MM-dd-yyyy HH-mm-ss-fff";

			LogManager.Setup().LoadConfiguration(static b => { // Setup NLog first
				b.ForLogger().FilterMinLevel(ConsoleLogLevel).WriteToColoredConsole(layout: LogLayout);
				b.ForLogger().FilterMinLevel(FileLogLevel).WriteToFile(fileName: $"{LogFolder}\\{DateTime.Now.ToString(LogDateFormat)}.log", layout: LogLayout);
			});

			Logger.Debug("Finished setting up NLog");

			ushort logFileLength = (ushort)(LogFolder.Length + LogDateFormat.Length + FileType.Length + 2);
			List<DateTime> dates = new();

			foreach (string file in Directory.GetFiles(LogFolder).Where(f => f.EndsWith($".{FileType}") && f.Length == logFileLength)) {
				if (DateTime.TryParseExact(file[(LogFolder.Length + 1)..^(FileType.Length + 1)], LogDateFormat, null, DateTimeStyles.None, out DateTime time)) { dates.Add(time); }
			}

			if (dates.Count >= MaxLogFiles) {
				dates.Sort(DateTime.Compare);

				Logger.Debug("Found too many log files. Deleting the oldest");
				while (dates.Count > MaxLogFiles) {
					File.Delete($"{LogFolder}\\{dates[0].ToString(LogDateFormat)}.log");
					dates.RemoveAt(0);
				}

				return;
			}

			Logger.Debug($"Found {dates.Count} logs");
		}
	}
}