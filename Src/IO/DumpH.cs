using Engine3.Utils;
using JetBrains.Annotations;
using NLog;
using ObjectLayoutInspector;

namespace Engine3.IO {
	[PublicAPI]
	public static class DumpH {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static string OutputFolder {
			get;
			set {
				if (wasSetup) { Logger.Warn($"{nameof(OutputFolder)} must be set before #{nameof(WriteDumpsToOutput)} is called"); }
				field = value;
			}
		} = "Output";

		public static string OutputFileType {
			get;
			set {
				if (wasSetup) { Logger.Warn($"{nameof(OutputFileType)} must be set before #{nameof(WriteDumpsToOutput)} is called"); }
				field = value;
			}
		} = "txt";

		private static Dictionary<string, string> ToWrite { get; } = new();
		private static bool wasSetup;

		public static event Action? AddDumps;

		internal static void WriteDumpsToOutput() {
			if (wasSetup) {
				Logger.Warn("Setup was already run");
				return;
			}

			Directory.CreateDirectory(OutputFolder);

			AddDefaultDumps();
			AddDumps?.Invoke();

			foreach ((string fileName, string content) in ToWrite) {
				using (FileStream f = File.Create($"{OutputFolder}/{fileName}.{OutputFileType}")) {
					using (StreamWriter w = new(f)) { w.Write(content); }
				}

				Logger.Debug($"- Wrote debug output file for {fileName}");
			}

			ToWrite.Clear();
			wasSetup = true;
		}

		private static void AddDefaultDumps() { AddStruct<Version4>(); }

		public static void AddStruct<T>() where T : struct => TryAdd(typeof(T).Name, $"{TypeLayout.GetLayout<T>()}");

		private static void TryAdd(string fileName, string content) {
			if (wasSetup) {
				Logger.Warn($"Attempted to add a debug output file to write too late. This must be set before calling {nameof(GameEngine)}#{nameof(GameEngine.Start)}");
				return;
			}

			if (!ToWrite.TryAdd(fileName, content)) { Logger.Warn($"Attempted to add a duplicate file: {fileName}"); }
		}
	}
}