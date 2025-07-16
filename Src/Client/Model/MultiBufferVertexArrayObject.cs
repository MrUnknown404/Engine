using OpenTK.Graphics.OpenGL4;

namespace Engine3.Client.Model {
	// https://www.khronos.org/opengl/wiki/Vertex_Specification_Best_Practices#Formatting_VBO_Data
	public class MultiBufferVertexArrayObject : IDisposable { // TODO figure out this class
		public uint Vao { get; private set; }
		public uint[] Buffers { get; private set; } = Array.Empty<uint>();

		public bool WasFreed { get; private set; }
		public bool WasVaoSet => Vao != 0;
		public bool WasBuffersSet => Buffers.Length != 0;

		public void GenBuffers() { }
		public void BindBuffers() { }

		protected virtual void Dispose(bool disposing) {
			if (WasFreed) { return; }
			if (!disposing) { return; }

			if (WasBuffersSet) {
				GL.DeleteBuffers(Buffers.Length, Buffers);
				for (int i = 0; i < Buffers.Length; i++) { Buffers[i] = 0; }
			}

			if (WasVaoSet) {
				GL.DeleteVertexArray(Vao);
				Vao = 0;
			}

			WasFreed = true;
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}