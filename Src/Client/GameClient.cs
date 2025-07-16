using Engine3.Utils;

namespace Engine3.Client {
	public abstract class GameClient {
		public Version4 Version { get; }
		public string StartupMessage { get; protected init; } = "Hello world";
		public string ExitMessage { get; protected init; } = "Goodbye world";

		protected event Action? OnSetupOpenGL;

		protected GameClient(Version4 version) => Version = version;

		internal void InvokeOnSetupOpenGL() => OnSetupOpenGL?.Invoke();

		public abstract void Update();
		public abstract void Render(float delta);
	}
}