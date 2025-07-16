using Engine3.Client;
using Engine3.Client.Model.Mesh.Vertex;
using NLog;
using ObjectLayoutInspector;

namespace Engine3.Utils {
	public static class DebugOutputH {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private static Dictionary<string, string> ToWrite { get; } = new();

		public static string OutputFolder {
			get;
			set {
				if (wasSetup) { Logger.Warn($"{nameof(OutputFolder)} must be set before #{nameof(Setup)} is called"); }
				field = value;
			}
		} = "Output";

		public static string OutputFileType {
			get;
			set {
				if (wasSetup) { Logger.Warn($"{nameof(OutputFileType)} must be set before #{nameof(Setup)} is called"); }
				field = value;
			}
		} = "txt";

		private static bool wasSetup;

		internal static void Setup<T>(T gameInstance) where T : GameClient {
			if (wasSetup) {
				Logger.Warn("Setup was already run");
				return;
			}

			Directory.CreateDirectory(OutputFolder);

			AddDefaults();
			gameInstance.AddDebugOutputs();

			WriteAllToFile();
			ToWrite.Clear();
			wasSetup = true;
		}

		private static void AddDefaults() {
			AddStruct<Version4>();
			AddStruct<VertexXyz>();
			AddStruct<VertexUv>();
			AddStruct<VertexXyzUv>();
		}

		private static void WriteAllToFile() {
			foreach ((string fileName, string content) in ToWrite) {
				using (FileStream f = File.Create($"{OutputFolder}\\{fileName}.{OutputFileType}")) {
					using (StreamWriter w = new(f)) { w.Write(content); }
				}

				Logger.Debug($"- Wrote debug output file for {fileName}");
			}
		}

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