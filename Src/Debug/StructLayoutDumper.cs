using Engine3.Utility.Extensions;
using Engine3.Utility.Versions;
using JetBrains.Annotations;
using NLog;
using ObjectLayoutInspector;

namespace Engine3.Debug {
	[PublicAPI]
	public static class StructLayoutDumper {
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

		public static event Action? AddStructs;

		internal static void WriteDumpsToOutput() {
			if (wasSetup) {
				Logger.Warn("Setup was already run");
				return;
			}

			Directory.CreateDirectory(OutputFolder);

			AddDefaultStructs();
			AddStructs?.Invoke();

			foreach ((string fileName, string content) in ToWrite) {
				using FileStream stream = File.Create($"{OutputFolder}/{fileName}.{OutputFileType}");
				using StreamWriter writer = new(stream);
				writer.Write(content);

				Logger.Debug($"- Wrote debug output file for {fileName}");
			}

			ToWrite.Clear();
			wasSetup = true;
		}

		private static void AddDefaultStructs() {
			AddStruct<Version4>();
			AddStruct<Version4<ushort>>();
			AddStruct<Version4Char>();
			AddStruct<Version4Char<ushort>>();
			AddStruct<Version4Interweaved>();
		}

		public static void AddStruct<T>() where T : struct => TryAdd(typeof(T).ToReadableName(), $"{TypeLayout.GetLayout<T>()}");

		private static void TryAdd(string fileName, string content) {
			if (wasSetup) {
				Logger.Warn($"Attempted to add a debug output file to write too late. This must be set before calling {nameof(GameClient)}#{nameof(GameClient.Start)}");
				return;
			}

			if (!ToWrite.TryAdd(fileName, content)) { Logger.Warn($"Attempted to add a duplicate file: {fileName}"); }
		}
	}
}