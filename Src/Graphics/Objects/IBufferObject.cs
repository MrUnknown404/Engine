using JetBrains.Annotations;

namespace Engine3.Graphics.Objects {
	[PublicAPI]
	public interface IBufferObject : IGraphicsResource {
		public void Copy<T>(ReadOnlySpan<T> data, ulong offset = 0) where T : unmanaged;
	}
}