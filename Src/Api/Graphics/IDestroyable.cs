using JetBrains.Annotations;
using NLog;

namespace Engine3.Api.Graphics {
	[PublicAPI]
	public interface IDestroyable {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public bool WasDestroyed { get; }

		public void Destroy();

		protected static bool WarnIfDestroyed<T>(T destroyable) where T : IDestroyable {
			if (destroyable.WasDestroyed) {
				Logger.Warn($"{typeof(T).Name} was already destroyed");
				return true;
			}

			return false;
		}
	}
}