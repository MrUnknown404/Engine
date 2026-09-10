using System.Text;
using JetBrains.Annotations;
using NLog;

namespace Engine4.IO;

public static class LoggerH {
	public const string SourcePropertyKey = "source";

	public const string Time = "[${processtime}]";
	public const string LogLevel = "[${level}]";
	public const string Thread = "[${threadname}]";
	public const string Source = $"[${{event-properties:{SourcePropertyKey}}}]";
	public const string Callsite = "[${callsite:includeNamespace=False}#${callsite-linenumber}]";
	public const string Message = "${message:exceptionSeparator= :withexception=true}";

	public static bool IsConsoleLoggingPaused { get; private set; }

	private static bool isSetup;

	internal static void Setup(LoggingSettings settings) {
		if (isSetup) { throw new Exception(); } // TODO exception

		Directory.CreateDirectory(settings.LogFileDirectory);

		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

		LogManager.Setup().SetupLogFactory(builder => {
			if (settings.TimeSource != null) { builder.SetTimeSource(settings.TimeSource); }
			builder.AddCallSiteHiddenClassType(typeof(LoggerH)); // not sure if this will break anything below
		}).LoadConfiguration(builder => {
			string? layout = settings.CustomLayout;
			if (layout == null) {
				StringBuilder withoutSource = new();
				StringBuilder withSource = new();

				if (settings.ShowTime) { withSource.Append($"{Time} "); }
				if (settings.ShowLogLevel) { withSource.Append($"{LogLevel} "); }
				if (settings.ShowThread) { withSource.Append($"{Thread} "); }

				if (settings.ShowSource) {
					withoutSource.Append(withSource);
					withSource.Append($"{Source} ");
				}

				if (settings.ShowCallsite) {
					withoutSource.Append($"{Callsite} ");
					withSource.Append($"{Callsite} ");
				}

				withoutSource.Append($"\\: {Message}");
				withSource.Append($"\\: {Message}");

				layout = $"${{when:when='${{event-properties:{SourcePropertyKey}}}'=='':inner={withoutSource}:else={withSource}}}";
			}

			if (settings.PrintToConsole) { builder.ForLogger().FilterMinLevel(settings.ConsoleLogLevel).FilterDynamicIgnore(static _ => IsConsoleLoggingPaused).WriteToColoredConsole(layout: layout); }

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

	public static void PauseConsoleLogging() => IsConsoleLoggingPaused = true;
	public static void UnpauseConsoleLogging() => IsConsoleLoggingPaused = false;

	[MustUseReturnValue]
	public static Logger GetLogger(LogSource? primarySource, string? secondarySource = null) {
		Logger logger = LogManager.GetCurrentClassLogger();
		if (primarySource is { } source) { logger = logger.WithProperty(SourcePropertyKey, secondarySource == null ? source.ToString() : $"{source.ToString()}.{secondarySource}"); }
		return logger;
	}

	private static void OnUnhandledException(object _, UnhandledExceptionEventArgs e) { // untested
		LogManager.GetCurrentClassLogger().Error(e.ExceptionObject as Exception, "Unhandled Exception"); // should i store current class logger?
		LogManager.Flush();
	}
}