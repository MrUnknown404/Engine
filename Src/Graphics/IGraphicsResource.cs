using NLog;

namespace Engine3.Graphics {
	public interface IGraphicsResource {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public string DebugName { get; }
		public bool WasDestroyed { get; }

		public void Destroy();

		public static bool CheckIfDestroyed<T>(T graphicsResource) where T : IGraphicsResource {
			if (graphicsResource.WasDestroyed) {
				Logger.Warn($"{graphicsResource.DebugName} ({typeof(T).Name}) was already destroyed");
				return true;
			}

			return false;
		}
	}
}