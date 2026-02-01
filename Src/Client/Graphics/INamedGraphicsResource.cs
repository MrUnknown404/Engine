using Engine3.Utility;
using JetBrains.Annotations;
using NLog;

namespace Engine3.Client.Graphics {
	[PublicAPI]
	public interface INamedGraphicsResource : IGraphicsResource, IDebugName {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		protected new bool WarnIfDestroyed<T>(T graphicsResource) where T : INamedGraphicsResource {
			if (graphicsResource.WasDestroyed) {
				Logger.Warn($"{graphicsResource.DebugName} ({typeof(T).Name}) was already destroyed");
				return true;
			}

			return false;
		}

		protected static void PrintNameWithHandle<T>(T resource, int handle) where T : INamedGraphicsResource => Logger.Trace($"{resource.DebugName} ({handle:X8})");
		protected static void PrintNameWithHandle<T>(T resource, ulong handle) where T : INamedGraphicsResource => Logger.Trace($"{resource.DebugName} ({handle:X16})");
	}
}