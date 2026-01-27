using JetBrains.Annotations;

namespace Engine3.Client.Graphics.Objects {
	[PublicAPI]
	public interface IBufferObject : IGraphicsResource {
		public void Copy<T>(ReadOnlySpan<T> data, ulong offset = 0) where T : unmanaged;
	}
}