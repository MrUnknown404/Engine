using Engine3.Utils;
using JetBrains.Annotations;

namespace Engine3.Api {
	[Obsolete]
	[PublicAPI]
	public interface IGameClient {
		public Version4 Version { get; }
		public string StartupMessage { get; }
		public string ExitMessage { get; }

		public void Update();
		public void Render(float delta);

		public bool IsCloseAllowed() => true;
	}
}