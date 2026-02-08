using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using NLog;

namespace Engine3.Client.Graphics {
	public abstract class GraphicsResource {
		protected bool WasDestroyed { get; set; }

		protected abstract void PrintCreate();
		protected abstract void PrintDestroy();
		protected abstract void WarnDestroy();

		protected abstract void Cleanup();

		internal void Destroy() {
			if (WasDestroyed) {
				WarnDestroy();
				return;
			}

			Cleanup();
			PrintDestroy();

			WasDestroyed = true;
		}
	}

	public abstract class GraphicsResource<TSelf, THandle> : GraphicsResource, IEquatable<TSelf>, IEquatable<GraphicsResource<TSelf, THandle>>
			where TSelf : GraphicsResource<TSelf, THandle> where THandle : unmanaged, IBinaryInteger<THandle> {
		[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		protected string TypeName { get; } = typeof(TSelf).Name;

		protected abstract THandle Handle { get; }

		protected override void PrintCreate() => Logger.Trace($"Creating {TypeName} (0x{Handle:X16})");
		protected override void PrintDestroy() => Logger.Trace($"Destroying {TypeName} (0x{Handle:X16})");
		protected override void WarnDestroy() => Logger.Warn($"{TypeName} (0x{Handle:X16}) was already destroyed");

		public bool Equals(GraphicsResource<TSelf, THandle>? other) => other != null && Handle == other.Handle;
		public bool Equals(TSelf? other) => other is GraphicsResource<TSelf, THandle> resource && Equals(resource);
		public override bool Equals(object? obj) => obj is GraphicsResource<TSelf, THandle> resource && Equals(resource);

		public override int GetHashCode() => Handle.GetHashCode();

		public static bool operator ==(GraphicsResource<TSelf, THandle>? left, GraphicsResource<TSelf, THandle>? right) => Equals(left, right);
		public static bool operator !=(GraphicsResource<TSelf, THandle>? left, GraphicsResource<TSelf, THandle>? right) => !Equals(left, right);
	}
}