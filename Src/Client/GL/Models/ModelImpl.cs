using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine2.Client.GL.Models.Vertex;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine2.Client.GL.Models {
	/// <summary> Contains shared code for <see cref="ImmutableModel{TVertex}"/> &amp; <see cref="MutableModel{TVertex}"/>. </summary>
	/// <remarks> You're probably looking for <see cref="Model"/> or the classes above. </remarks>
	public abstract class ModelImpl<TVertex> : Model where TVertex : IVertex {
		protected abstract IEnumerable<Mesh<TVertex>> BuildMesh { get; }
		protected abstract IEnumerable<BufferData> BuiltMesh { get; }

		internal ModelImpl() { }

		protected internal override void Draw() {
			foreach (BufferData data in BuiltMesh) { // i think there may be some boxing issues with this. dunno. i noticed an extra IEnumerable in memory after this runs
				OpenGL4.BindBuffer(BufferTarget.ElementArrayBuffer, data.EBO);
				OpenGL4.DrawElements(PrimitiveType.Triangles, data.Count, DrawElementsType.UnsignedInt, 0);
			}
		}

		protected void BindToBuffer(uint vbo, uint ebo, float[] vertices, uint[] indices) {
			OpenGL4.BindBuffer(BufferTarget.ArrayBuffer, vbo);
			OpenGL4.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferHint);
			BindVertexAttribs();
			OpenGL4.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
			OpenGL4.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferHint);
		}

		private static void BindVertexAttribs() {
			byte offset = 0;
			for (uint attribIndex = 0; attribIndex < TVertex.Arrangement.Length; attribIndex++) {
				byte vertSize = TVertex.Arrangement[attribIndex];

				OpenGL4.EnableVertexAttribArray(attribIndex);
				OpenGL4.VertexAttribPointer(attribIndex, vertSize, VertexAttribPointerType.Float, false, sizeof(float) * TVertex.Length, sizeof(float) * offset);

				offset += vertSize;
			}
		}

		public override void Free() {
			base.Free();
			foreach (BufferData data in BuiltMesh) {
				OpenGL4.DeleteBuffer(data.VBO);
				OpenGL4.DeleteBuffer(data.EBO);
			}
		}

		protected sealed class BufferData {
			public required uint VBO { get; init; }
			public required uint EBO { get; init; }
			public required int Count { get; init; } // int ):
		}
	}
}