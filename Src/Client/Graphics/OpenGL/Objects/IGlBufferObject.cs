using Engine3.Client.Graphics.Objects;
using JetBrains.Annotations;
using OpenTK.Graphics;

namespace Engine3.Client.Graphics.OpenGL.Objects {
	[PublicAPI]
	public interface IGlBufferObject : IBufferObject {
		public BufferHandle Handle { get; }
	}
}