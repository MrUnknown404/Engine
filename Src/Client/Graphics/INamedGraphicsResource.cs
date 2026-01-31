using Engine3.Utility;
using JetBrains.Annotations;
using NLog;

namespace Engine3.Client.Graphics {
	[PublicAPI]
	public interface INamedGraphicsResource : IGraphicsResource, IDebugName {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		protected new static bool WarnIfDestroyed<T>(T graphicsResource) where T : INamedGraphicsResource {
			if (graphicsResource.WasDestroyed) {
				Logger.Warn($"{graphicsResource.DebugName} ({typeof(T).Name}) was already destroyed");
				return true;
			}

			return false;
		}
	}
}