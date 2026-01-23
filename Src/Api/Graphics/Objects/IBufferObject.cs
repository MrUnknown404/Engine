using System.Numerics;

namespace Engine3.Api.Graphics.Objects {
	public interface IBufferObject<TBufferSize> : IGraphicsResource where TBufferSize : IBinaryInteger<TBufferSize> {
		public TBufferSize BufferSize { get; }

		public void Copy<T>(T[] data, TBufferSize offset) where T : unmanaged;
	}
}