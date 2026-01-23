using NLog;

namespace Engine3.Graphics {
	public interface IGraphicsResource : IDestroyable {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public string DebugName { get; }

		protected new static bool WarnIfDestroyed<T>(T graphicsResource) where T : IGraphicsResource {
			if (graphicsResource.WasDestroyed) {
				Logger.Warn($"{graphicsResource.DebugName} ({typeof(T).Name}) was already destroyed");
				return true;
			}

			return false;
		}
	}
}