using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using NLog;

namespace Engine3.Client.Graphics {
	public abstract class NamedGraphicsResource<TSelf, THandle> : GraphicsResource<TSelf, THandle> where TSelf : GraphicsResource<TSelf, THandle> where THandle : unmanaged, IBinaryInteger<THandle> {
		[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public string DebugName { get; }

		protected NamedGraphicsResource(string debugName) => DebugName = debugName;

		protected override void PrintCreate() => Logger.Trace($"Creating {TypeName} ({DebugName}, 0x{Handle:X16})");
		protected override void PrintDestroy() => Logger.Trace($"Destroying {TypeName} ({DebugName}, 0x{Handle:X16})");
		protected override void WarnDestroy() => Logger.Warn($"{TypeName} ({DebugName}, 0x{Handle:X16}) was already destroyed");
	}
}