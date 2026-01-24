using Engine3.Graphics.Objects;
using JetBrains.Annotations;
using OpenTK.Graphics;

namespace Engine3.Graphics.OpenGL.Objects {
	[PublicAPI]
	public interface IGlBufferObject : IBufferObject {
		public BufferHandle Handle { get; }

		public void Copy<T>(T[] data, nint offset) where T : unmanaged;
	}
}