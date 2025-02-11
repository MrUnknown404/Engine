using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine2.Client.Models.Vertex;

namespace USharpLibs.Engine2.Client.Models.Meshes {
	public abstract class Mesh {
		protected internal abstract int IndexCount { get; }

		protected internal abstract void BindToBuffer(uint vbo, uint ebo, BufferUsageHint bufferHint);
		protected abstract void BindIndices(uint ebo, BufferUsageHint bufferHint);

		protected static byte[] CollectVertexData<TVertex>(IList<TVertex> data) where TVertex : struct, IVertex {
			byte[] vertices = new byte[data.Count * TVertex.SizeInBytes];
			for (int vertexIndex = 0; vertexIndex < data.Count; vertexIndex++) {
				data[vertexIndex].Collect(ref vertices, vertexIndex * TVertex.SizeInBytes); // i tested various pointer versions but this seems to be the fastest
			}

			return vertices;
		}

		protected static void BindVertexAttrib(uint attribIndex, VertexLayout layout, byte size, ref byte offset) {
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

		protected static void BindSubBuffer<T>(IList<T> data, byte attribIndex, ref int bufferOffset, ref byte attribOffset) where T : struct, IVertexAttribute {
			int size = data.Count * T.SizeInBytes;
			GL.BufferSubData(BufferTarget.ArrayBuffer, bufferOffset, size, CollectVertexData(data));
			BindVertexAttrib(attribIndex, T.VertexLayout, T.SizeInBytes, ref attribOffset);
			bufferOffset += size;
		}
	}
}