using OpenTK.Graphics.OpenGL4;

namespace USharpLibs.Engine2.Client.Models {
	public interface IMesh {
		public int IndexCount { get; }
		public void BindToBuffer(uint vbo, uint ebo, BufferUsageHint bufferHint);
	}
}