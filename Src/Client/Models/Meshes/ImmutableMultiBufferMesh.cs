using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine2.Client.Models.Vertex;
using USharpLibs.Engine2.Debug;

namespace USharpLibs.Engine2.Client.Models.Meshes {
	public class ImmutableMultiBufferMesh<T0> : ImmutableMesh where T0 : struct, IVertexAttribute {
		private T0[] Data0 { get; }

		public ImmutableMultiBufferMesh(uint[] indices, T0[] data0) : base(indices) {
			_ = MeshErrorHandler.Assert(data0.Length == 0, static () => new(MeshErrorHandler.Reason.EmptyVertexArray));

			Data0 = data0;
		}

		protected internal override void BindToBuffer(uint vbo, uint ebo, BufferUsageHint bufferHint) {
			BindArrayBuffer(vbo, Data0.Length * T0.SizeInBytes, bufferHint);

			int bufferOffset = 0;
			byte attribOffset = 0;

			BindSubBuffer(Data0, 0, ref bufferOffset, ref attribOffset);

			BindIndices(ebo, bufferHint);
		}
	}

	public class ImmutableMultiBufferMesh<T0, T1> : ImmutableMesh where T0 : struct, IVertexAttribute where T1 : struct, IVertexAttribute {
		private T0[] Data0 { get; }
		private T1[] Data1 { get; }

		public ImmutableMultiBufferMesh(uint[] indices, T0[] data0, T1[] data1) : base(indices) {
			_ = MeshErrorHandler.Assert(data0.Length == 0, static () => new(MeshErrorHandler.Reason.EmptyVertexArray));
			_ = MeshErrorHandler.Assert(data1.Length == 0, static () => new(MeshErrorHandler.Reason.EmptyVertexArray));

			int length = data0.Length;
			_ = MeshErrorHandler.Assert(data1.Length != length, static () => new(MeshErrorHandler.Reason.IncorrectlySizedVertexArray));

			Data0 = data0;
			Data1 = data1;
		}

		protected internal override void BindToBuffer(uint vbo, uint ebo, BufferUsageHint bufferHint) {
			BindArrayBuffer(vbo, Data0.Length * T0.SizeInBytes + Data1.Length * T1.SizeInBytes, bufferHint);

			int bufferOffset = 0;
			byte attribOffset = 0;

			BindSubBuffer(Data0, 0, ref bufferOffset, ref attribOffset);
			BindSubBuffer(Data1, 1, ref bufferOffset, ref attribOffset);

			BindIndices(ebo, bufferHint);
		}
	}
}