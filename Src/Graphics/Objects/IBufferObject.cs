using JetBrains.Annotations;

namespace Engine3.Graphics.Objects {
	[PublicAPI]
	public interface IBufferObject : IGraphicsResource {
		public void Copy<T>(T[] data) where T : unmanaged;
	}
}