using Engine3.Client.Vertex;
using NLog;
using ObjectLayoutInspector;

namespace Engine3.Utils {
	public static class DebugOutputH {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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

		internal static void Setup() {
			if (wasSetup) {
				Logger.Warn("Setup was already run");
				return;
			}

			Directory.CreateDirectory(OutputFolder);

			AddStruct<VertexXyz>();
			AddStruct<VertexUv>();
			AddStruct<VertexXyzUv>();

			wasSetup = true;
		}

		private static void WriteToFile<T>(string fileName, ReadOnlySpan<char> content) {
			FileStream f = File.Create($"{OutputFolder}\\{fileName}.{OutputFileType}");
			using (StreamWriter w = new(f)) { w.Write(content); }
			Logger.Debug($"Wrote debug output file for {typeof(T).Name}");
		}

		public static void AddStruct<T>() where T : struct => WriteToFile<T>(typeof(T).Name, $"{TypeLayout.GetLayout<T>()}");
	}
}