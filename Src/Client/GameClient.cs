using Engine3.Utils;

namespace Engine3.Client {
	public abstract class GameClient {
		public Version4 Version { get; }
		public string StartupMessage { get; protected init; } = "Hello world!";
		public string ExitMessage { get; protected init; } = "Goodbye world!";

		protected GameClient(Version4 version) => Version = version;

		protected internal abstract void SetupOpenGL();

		public void OnRender(float elapsed) { }
	}
}