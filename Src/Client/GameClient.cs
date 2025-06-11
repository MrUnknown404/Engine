using Engine3.Utils;

namespace Engine3.Client {
	public abstract class GameClient {
		public Version4 Version { get; }
		public bool EnableDebugOutputs { get; protected init; }
		public string StartupMessage { get; protected init; } = "Hello world";
		public string ExitMessage { get; protected init; } = "Goodbye world";

		protected event Action? OnSetupOpenGL;

		protected GameClient(Version4 version) => Version = version;

		internal void InvokeOnSetupOpenGL() => OnSetupOpenGL?.Invoke();

		protected internal abstract void Update();
		protected internal abstract void Render(float delta);
		protected internal virtual void AddDebugOutputs() { }
	}
}