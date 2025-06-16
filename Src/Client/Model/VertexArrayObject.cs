using OpenTK.Graphics.OpenGL4;

namespace Engine3.Client.Model {
	public class VertexArrayObject : IDisposable {
		public uint Vao { get; private set; }
		public uint Vbo { get; private set; }
		public uint Ebo { get; private set; }

		public bool WasFreed { get; private set; }
		public bool WereAttributesBound { get; internal set; }
		public bool WasVaoSet => Vao != 0;
		public bool WasVboSet => Vbo != 0;
		public bool WasEboSet => Ebo != 0;

		public void GenBuffers() {
			if (WasFreed) { throw new Exception(); } // TODO handle/exception
			if (WasVaoSet) { throw new Exception(); } // TODO handle/exception

			Vao = (uint)GL.GenVertexArray();
			Vbo = (uint)GL.GenBuffer();
			Ebo = (uint)GL.GenBuffer();
		}

		private void Dispose(bool disposing) {
			if (WasFreed) { return; }
			if (!disposing) { return; }

			// ReSharper disable once ConvertIfStatementToSwitchStatement
			if (WasVboSet && WasEboSet) {
				GL.DeleteBuffers(2, [ Vbo, Ebo, ]); // tbh i don't think this is necessary. i bet my checks are slower than just running glDeleteBuffer twice. why am i like this
			} else if (WasVboSet) {
				GL.DeleteBuffer(Vbo); //
			} else if (WasEboSet) {
				GL.DeleteBuffer(Ebo); //
			}

			if (WasVaoSet) { GL.DeleteVertexArray(Vao); }

			Vao = 0;
			Vbo = 0;
			Ebo = 0;
			WasFreed = true;
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}