using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine2.Client.Models.Vertex;
using USharpLibs.Engine2.Debug;

namespace USharpLibs.Engine2.Client.Models {
	public abstract class Mesh<TIndices> where TIndices : IList<uint> {
		private TIndices Indices { get; }
		protected internal int IndexCount => Indices.Count;

		protected Mesh(TIndices indices) {
			_ = MeshErrorHandler.Assert(indices.Count % 3 != 0, static () => new(MeshErrorHandler.Reason.IncorrectlySizedIndexArray)); // TODO support other draw types. (triangle strips are 4 indices per)

			Indices = indices;
		}

		protected internal abstract void BindToBuffer(uint vbo, uint ebo, BufferUsageHint bufferHint);

		protected void BindIndices(uint ebo, BufferUsageHint bufferHint) {
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
			GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Count * sizeof(uint), Indices.ToArray(), bufferHint);
		}

		protected static byte[] CollectVertexData<TVertex>(IList<TVertex> data) where TVertex : struct, IVertex {
			byte[] vertices = new byte[data.Count * TVertex.SizeInBytes];
			for (int vertexIndex = 0; vertexIndex < data.Count; vertexIndex++) {
				data[vertexIndex].Collect(ref vertices, vertexIndex * TVertex.SizeInBytes); // i tested various pointer versions but this seems to be the fastest
			}

			return vertices;
		}

		protected static void BindVertexAttrib(uint attribIndex, VertexAttribLayout layout, byte size, ref byte offset) {
			GL.EnableVertexAttribArray(attribIndex);
			GL.VertexAttribPointer(attribIndex, layout.ElementCount, layout.VertexAttribType, false, size, offset);
			offset += layout.TypeToByteCount;
		}

		protected static void BindArrayBuffer(uint buffer, byte[] data, int size, BufferUsageHint bufferHint) {
			GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
			GL.BufferData(BufferTarget.ArrayBuffer, size, data, bufferHint);
		}

		protected static void BindArrayBuffer(uint buffer, int size, BufferUsageHint bufferHint) {
			GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
			GL.BufferData(BufferTarget.ArrayBuffer, size, IntPtr.Zero, bufferHint);
		}

		protected static void BindSubBuffer<TVertex>(IList<TVertex> data, byte attribIndex, ref int bufferOffset, ref byte attribOffset) where TVertex : struct, IVertexAttribute {
			int size = data.Count * TVertex.SizeInBytes;
			GL.BufferSubData(BufferTarget.ArrayBuffer, bufferOffset, size, CollectVertexData(data));
			BindVertexAttrib(attribIndex, TVertex.VertexLayout, TVertex.SizeInBytes, ref attribOffset);
			bufferOffset += size;
		}
	}
}