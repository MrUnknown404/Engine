using OpenTK.Graphics.OpenGL4;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine.Client.GL.Mesh {
	public abstract class RawMesh {
		public int VAO { get; protected set; }
		public bool WasSetup { get; protected set; }

		protected MeshData MeshData { get; } = new();
		protected BufferUsageHint BufferHint { get; }
		protected int VBO { get; set; }
		protected int EBO { get; set; }
		protected int Count { get; set; }

		protected RawMesh(BufferUsageHint bufferHint) => BufferHint = bufferHint;

		public void Draw() {
			if (!WasSetup) {
				ClientBase.Logger.WarnLine("Mesh was not setup!");
				return;
			} else if (GLH.CurrentVAO != VAO) {
				ClientBase.Logger.WarnLine("Mesh is not bound!");
				return;
			}

			IDraw();
		}

		public abstract void SetupGL();

		protected virtual void IDraw() => OpenGL4.DrawElements(PrimitiveType.Triangles, Count, DrawElementsType.UnsignedInt, 0);

		public virtual void Reset() {
			MeshData.Reset();
			Count = 0;
		}
	}
}