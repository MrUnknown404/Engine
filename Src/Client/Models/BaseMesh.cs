using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine2.Debug;

namespace USharpLibs.Engine2.Client.Models {
	public abstract class BaseMesh<TIndex> : IMesh where TIndex : IList<uint> {
		public TIndex Indices { get; }
		public int IndexCount => Indices.Count;

		internal BaseMesh(TIndex indices, [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")] bool allowEmptyData) { // atm i don't have a better way of doing this
			_ = MeshErrorHandler.Assert(!allowEmptyData && indices.Count == 0, static () => new(MeshErrorHandler.Reason.EmptyIndexArray));
			_ = MeshErrorHandler.Assert(indices.Count % 3 != 0, static () => new(MeshErrorHandler.Reason.IncorrectlySizedIndexArray));

			Indices = indices;
		}

		public abstract void BindToBuffer(uint vbo, uint ebo, BufferUsageHint bufferHint);

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

		protected void BindIndices(uint ebo, BufferUsageHint bufferHint) {
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
			GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Count * sizeof(uint), Indices as uint[] ?? Indices.ToArray(), bufferHint);
		}
	}
}