using JetBrains.Annotations;

namespace Engine3.Client.Graphics {
	[PublicAPI]
	public interface IBufferObject : INamedGraphicsResource {
		public ulong BufferSize { get; }
		public void Copy<T>(ReadOnlySpan<T> data, ulong offset = 0) where T : unmanaged;
	}
}