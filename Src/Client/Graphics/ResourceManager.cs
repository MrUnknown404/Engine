using System.Diagnostics.CodeAnalysis;
using NLog;

namespace Engine3.Client.Graphics {
	public class ResourceManager<TSelf> where TSelf : GraphicsResource {
		[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private List<TSelf> Resources { get; } = new();
		private Queue<TSelf> DeletionQueue { get; } = new();

		public void Add(TSelf toAdd) => Resources.Add(toAdd);
		public void EnqueueDestroy(TSelf toDestroy) => DeletionQueue.Enqueue(toDestroy);

		public void TryCleanup() {
			if (DeletionQueue.Count == 0) { return; }

			while (DeletionQueue.TryDequeue(out TSelf? obj)) {
				if (Resources.Remove(obj)) { obj.Destroy(); } else { Logger.Error($"Could not find to be destroyed {typeof(TSelf).Name}"); }
			}
		}

		public void CleanupAll() {
			if (Resources.Count == 0) { return; }

			Logger.Debug($"Cleaning up {Resources.Count} resources...");
			foreach (TSelf resource in Resources) { resource.Destroy(); }
		}
	}
}