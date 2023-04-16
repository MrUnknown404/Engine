using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using USharpLibs.Common.Utils;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine.Client.GL.OldMesh {
	[Obsolete("Going to be removed.")]
	public interface IRawMesh {
		public int VAO { get; }
		public bool WasSetup { get; }

		public void SetupGL();
		public void Draw();
		public void Reset();
	}

	[Obsolete("Going to be removed.")]
	[PublicAPI]
	public abstract class RawMesh<T> : IRawMesh where T : IMeshData, new() {
		public int VAO { get; protected set; }
		public bool WasSetup { get; protected set; }

		protected T MeshData { get; } = new();
		protected BufferUsageHint BufferHint { get; }
		protected int VBO { get; set; }
		protected int EBO { get; set; }
		protected int Count { get; set; }

		protected RawMesh(BufferUsageHint bufferHint) => BufferHint = bufferHint;

		public abstract void SetupGL();

		public void Draw() {
			if (!WasSetup) {
				Logger.Warn("Mesh was not setup!");
				return;
			} else if (GLH.CurrentVAO != VAO) {
				Logger.Warn("Mesh is not bound!");
				return;
			}

			IDraw();
		}

		protected virtual void IDraw() => OpenGL4.DrawElements(PrimitiveType.Triangles, Count, DrawElementsType.UnsignedInt, 0);

		public virtual void Reset() {
			MeshData.Reset();
			Count = 0;
		}
	}
}