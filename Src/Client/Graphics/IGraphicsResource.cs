using Engine3.Utility;
using JetBrains.Annotations;
using NLog;

namespace Engine3.Client.Graphics {
	[PublicAPI]
	public interface IGraphicsResource : IDestroyable {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		protected static void PrintNameWithHandle<T>(int handle) where T : IGraphicsResource => Logger.Trace($"{typeof(T).Name} (0x{handle:X8})");
		protected static void PrintNameWithHandle<T>(ulong handle) where T : IGraphicsResource => Logger.Trace($"{typeof(T).Name} (0x{handle:X16})");
	}
}