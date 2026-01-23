using System.Numerics;

namespace Engine3.Api.Graphics.Objects {
	public interface IBufferObject : IGraphicsResource {
		public void Copy<T>(T[] data) where T : unmanaged;
	}

	public interface IBufferObject<TBufferSize> : IBufferObject where TBufferSize : IBinaryInteger<TBufferSize> {
		public TBufferSize BufferSize { get; }

		public void Copy<T>(T[] data, TBufferSize offset) where T : unmanaged;
	}
}