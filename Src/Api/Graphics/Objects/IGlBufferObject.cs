using JetBrains.Annotations;
using OpenTK.Graphics;

namespace Engine3.Api.Graphics.Objects {
	[PublicAPI]
	public interface IGlBufferObject : IBufferObject {
		public BufferHandle Handle { get; }

		public void Copy<T>(T[] data, nint offset) where T : unmanaged;
	}
}