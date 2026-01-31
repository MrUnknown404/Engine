using JetBrains.Annotations;
using NLog;
using NLog.Time;

namespace Engine3.Utility {
	[PublicAPI]
	public static class LoggerH {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static ushort MaxFiles {
			get;
			set {
				if (wasSetup) { Logger.Warn($"{nameof(MaxFiles)} must be set before #{nameof(Setup)} is called"); }
				field = value;
			}
		} = 5;

		public static string LogFolder {
			get;
			set {
				if (wasSetup) { Logger.Warn($"{nameof(LogFolder)} must be set before #{nameof(Setup)} is called"); }
				field = value;
			}
		} = "Logs";

		public static string LogFileType {
			get;
			set {
				if (wasSetup) { Logger.Warn($"{nameof(LogFileType)} must be set before #{nameof(Setup)} is called"); }
				field = value;
			}
		} = "log";

		/// <summary> See https://nlog-project.org/config/?tab=layout-renderers </summary>
		public static string LogLayout {
			get;
			set {
				if (wasSetup) { Logger.Warn($"{nameof(LogLayout)} must be set before #{nameof(Setup)} is called"); }
				field = value;
			}
		} = "[${processtime}] [${level}] [${callsite:includeNamespace=False}#${callsite-linenumber}] ${message:exceptionSeparator=:withexception=true}";

		public static LogLevel ConsoleLogLevel {
			get;
			set {
				if (wasSetup) { Logger.Warn($"{nameof(LogLayout)} must be set before #{nameof(Setup)} is called"); }
				field = value;
			}
		} = LogLevel.Debug;

		public static LogLevel FileLogLevel {
			get;
			set {
				if (wasSetup) { Logger.Warn($"{nameof(LogLayout)} must be set before #{nameof(Setup)} is called"); }
				field = value;
			}
		} = LogLevel.Debug;

		private static bool wasSetup;

		internal static void Setup() {
			if (wasSetup) {
				Logger.Warn($"Running {nameof(LoggerH)}#{nameof(Setup)} twice is not supported");
				return;
			}

			const string LogDateFormat = "MM-dd-yyyy HH-mm-ss-fff";

			Directory.CreateDirectory(LogFolder);

			TimeSource.Current = new AccurateUtcTimeSource();
			LogManager.Setup().SetupLogFactory(static s => {
				s.AddCallSiteHiddenClassType(typeof(LoggerH)); //
			}).LoadConfiguration(static b => {
				b.ForLogger().FilterMinLevel(ConsoleLogLevel).WriteToColoredConsole(layout: LogLayout);
				b.ForLogger().FilterMinLevel(FileLogLevel).WriteToFile(fileName: $"{LogFolder}/{DateTime.Now.ToString(LogDateFormat)}.{LogFileType}", layout: LogLayout, maxArchiveFiles: MaxFiles - 1);
			});

			AppDomain.CurrentDomain.UnhandledException += static (_, args) => Logger.Error((Exception)args.ExceptionObject, "Uncaught Exception: ");

			wasSetup = true;
		}
	}
}