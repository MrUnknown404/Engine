using JetBrains.Annotations;

namespace Engine3.Api.Graphics.Objects {
	[PublicAPI]
	public interface IBufferObject : IGraphicsResource {
		public void Copy<T>(T[] data) where T : unmanaged;
	}
}