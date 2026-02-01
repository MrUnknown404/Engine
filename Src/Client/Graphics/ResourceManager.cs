using NLog;

namespace Engine3.Client.Graphics {
	public class ResourceManager<T> where T : IGraphicsResource, IEquatable<T> {
		// ReSharper disable once StaticMemberInGenericType
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

#if DEBUG
		public ResourceManager() {
			bool isNamedResourceManager = (GetType().BaseType ?? throw new NullReferenceException()).Name[..^2] == nameof(ResourceManager<>);
			bool isTypeNamed = typeof(T).GetInterfaces().Contains(typeof(INamedGraphicsResource));
			if (!isNamedResourceManager && isTypeNamed) { Logger.Warn($"{typeof(T).Name} is of type {nameof(INamedGraphicsResource)} and should be in {nameof(NamedResourceManager<>)}"); }
		}
#endif

		protected List<T> Resources { get; } = new();
		protected Queue<T> DeletionQueue { get; } = new();

		public void Add(T toAdd) => Resources.Add(toAdd);
		public void Destroy(T toDestroy) => DeletionQueue.Enqueue(toDestroy);

		public virtual void TryCleanup() {
			if (DeletionQueue.Count != 0) {
				while (DeletionQueue.TryDequeue(out T? obj)) {
					if (Resources.Remove(obj)) {
						Logger.Trace($"Destroying: {obj.GetType().Name}");
						obj.Destroy();
					} else { Logger.Error($"Could not find to be destroyed {typeof(T).Name}"); }
				}
			}
		}

		public void CleanupAll() {
			if (Resources.Count == 0) { return; }

			Logger.Debug($"Cleaning up {Resources.Count} resources...");
			foreach (T resource in Resources) { DeletionQueue.Enqueue(resource); }
			TryCleanup();
		}
	}
}