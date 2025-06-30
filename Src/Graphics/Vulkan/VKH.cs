using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics;

namespace Engine3.Graphics.Vulkan {
	[PublicAPI]
	public static class VKH { // TODO impl
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static bool WereBindingsLoaded { get; private set; }

		internal static void Setup() {
			VKLoader.Init();
			WereBindingsLoaded = true;
		}

		internal static void Render() { }
	}
}