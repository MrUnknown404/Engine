using NLog;

namespace Engine3.Graphics.OpenGL {
	public static partial class GLH {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		/// <summary> Note: This must be set before you set up OpenGL </summary>
		public static List<uint> DisabledCallbackIds { get; } = new();
	}
}