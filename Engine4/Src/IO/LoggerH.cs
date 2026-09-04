using System.Text;
using JetBrains.Annotations;
using NLog;
using NLog.Time;

namespace Engine4.IO;

public static class LoggerH {
	public const string SourcePropertyKey = "source";

	public const string Time = "[${processtime}]";
	public const string LogLevel = "[${level}]";
	public const string Thread = "[${threadname}]";
	public const string Source = $"[${{event-properties:{SourcePropertyKey}}}]";
	public const string Callsite = "[${callsite:includeNamespace=False}#${callsite-linenumber}]";
	public const string Message = "${message:exceptionSeparator= :withexception=true}";

	private static bool isSetup;

	internal static void Setup(LoggingSettings settings) {
		if (isSetup) { throw new Exception(); } // TODO exception

		// TODO https://github.com/NLog/NLog/wiki/Tutorial#5-remember-to-flush

		Directory.CreateDirectory(settings.LogFileDirectory);

		if (settings.TimeSource != null) { TimeSource.Current = settings.TimeSource; }

		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

		LogManager.Setup().LoadConfiguration(builder => {
			string? layout = settings.CustomLayout;
			if (layout == null) {
				StringBuilder withoutSource = new();
				StringBuilder withSource = new();

				if (settings.ShowTime) {
					withoutSource.Append($"{Time} ");
					withSource.Append($"{Time} ");
				}

				if (settings.ShowLogLevel) {
					withoutSource.Append($"{LogLevel} ");
					withSource.Append($"{LogLevel} ");
				}

				if (settings.ShowThread) {
					withoutSource.Append($"{Thread} ");
					withSource.Append($"{Thread} ");
				}

				if (settings.ShowSource) { withSource.Append($"{Source} "); }

				if (settings.ShowCallsite) {
					withoutSource.Append($"{Callsite} ");
					withSource.Append($"{Callsite} ");
				}

				withoutSource.Append($"\\: {Message}");
				withSource.Append($"\\: {Message}");

				layout = $"${{when:when='${{event-properties:{SourcePropertyKey}}}'=='':inner={withoutSource}:else={withSource}}}";
			}

			if (settings.PrintToConsole) { builder.ForLogger().FilterMinLevel(settings.ConsoleLogLevel).WriteToColoredConsole(layout: layout); }

			if (settings.PrintToFile) {
				builder.ForLogger().FilterMinLevel(settings.FileLogLevel).WriteToFile(layout: layout, fileName: $"{settings.LogFileDirectory}/{DateTime.Now.ToString(settings.LogFileDateFormat)}.{settings.LogFileExtension}",
					maxArchiveFiles: settings.MaxLogFiles - 1);
			}

			// database output?
		});

		isSetup = true;
	}

	internal static void Shutdown() {
		if (!isSetup) { throw new Exception(); } // TODO exception

		AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
		LogManager.Shutdown();

		isSetup = false;
	}

	[MustUseReturnValue]
	public static Logger GetLogger(LogSource? primarySource, string? secondarySource = null) {
		Logger logger = LogManager.GetCurrentClassLogger();
		if (primarySource is { } source) { logger = logger.WithProperty(SourcePropertyKey, secondarySource == null ? source.ToString() : $"{source.ToString()}.{secondarySource}"); }
		return logger;
	}

	private static void OnUnhandledException(object _, UnhandledExceptionEventArgs e) {
		LogManager.GetCurrentClassLogger().Error(e.ExceptionObject as Exception, "Unhandled Exception"); // should i store current class logger?
		LogManager.Flush();
	}
}