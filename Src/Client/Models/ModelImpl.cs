using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine2.Client.Models.Vertex;

namespace USharpLibs.Engine2.Client.Models {
	/// <summary> Contains shared code for <see cref="ImmutableModel{TVertex}"/> &amp; <see cref="MutableModel{TVertex}"/>. </summary>
	/// <remarks> You're probably looking for <see cref="Model"/> or the classes above. </remarks>
	public abstract class ModelImpl<TVertex> : Model where TVertex : IVertex {
		protected abstract IEnumerable<Mesh<TVertex>> BuildMesh { get; }
		protected abstract IEnumerable<BufferData> BuiltMesh { get; }

		internal ModelImpl() { }

		protected internal override void Draw() {
			foreach (BufferData data in BuiltMesh) { // i think there may be some boxing issues with this. dunno. i noticed an extra IEnumerable in memory after this runs
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, data.EBO);
				GL.DrawElements(PrimitiveType.Triangles, data.Count, DrawElementsType.UnsignedInt, 0);
			}
		}

		protected void BindToBuffer(uint vbo, uint ebo, float[] vertices, uint[] indices) {
			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
			GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferHint);
			BindVertexAttribs();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
			GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferHint);
		}

		private static void BindVertexAttribs() {
			byte offset = 0;
			for (uint attribIndex = 0; attribIndex < TVertex.Arrangement.Length; attribIndex++) {
				byte vertSize = TVertex.Arrangement[attribIndex];

				GL.EnableVertexAttribArray(attribIndex);
				GL.VertexAttribPointer(attribIndex, vertSize, VertexAttribPointerType.Float, false, sizeof(float) * TVertex.Length, sizeof(float) * offset);

				offset += vertSize;
			}
		}

		public override void Free() {
			base.Free();
			foreach (BufferData data in BuiltMesh) {
				GL.DeleteBuffer(data.VBO);
				GL.DeleteBuffer(data.EBO);
			}
		}

		protected sealed class BufferData {
			public required uint VBO { get; init; }
			public required uint EBO { get; init; }
			public required int Count { get; init; } // int ):
		}
	}
}