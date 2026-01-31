using NLog;

namespace Engine3.Client.Graphics {
	public class NamedResourceManager<T> : ResourceManager<T> where T : INamedGraphicsResource, IEquatable<T> {
		// ReSharper disable once StaticMemberInGenericType
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public override void TryCleanup() {
			if (DeletionQueue.Count != 0) {
				while (DeletionQueue.TryDequeue(out T? obj)) {
					if (Resources.Remove(obj)) {
						Logger.Trace($"Destroying: {obj.DebugName}");
						obj.Destroy();
					} else { Logger.Error($"Could not find to be destroyed {typeof(T).Name}"); }
				}
			}
		}
	}
}