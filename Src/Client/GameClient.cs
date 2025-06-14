using Engine3.Utils;

namespace Engine3.Client {
	public abstract class GameClient {
		public Version4 Version { get; }
		public string StartupMessage { get; protected init; } = "Hello world";
		public string ExitMessage { get; protected init; } = "Goodbye world";

		protected GameClient(Version4 version) => Version = version;

		protected internal abstract void Update();
		protected internal abstract void Render(float delta);

		protected internal virtual void AddDebugOutputs() { }
		protected internal virtual bool IsCloseAllowed() => true;
	}
}