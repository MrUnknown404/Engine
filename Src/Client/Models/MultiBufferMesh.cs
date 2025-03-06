using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine2.Client.Models.Vertex;

namespace USharpLibs.Engine2.Client.Models {
	public class MultiBufferMesh<T0> : Mesh<List<uint>> where T0 : struct, IVertexAttribute {
		private List<T0> Data0 { get; } = new();

		public MultiBufferMesh() : base(new()) { }

		protected internal override void BindToBuffer(uint vbo, uint ebo, BufferUsageHint bufferHint) {
			BindArrayBuffer(vbo, Data0.Count * T0.SizeInBytes, bufferHint);

			int bufferOffset = 0;
			byte attribOffset = 0;

			BindSubBuffer(Data0, 0, ref bufferOffset, ref attribOffset);

			BindIndices(ebo, bufferHint);
		}

		// TODO mutation methods & check!
	}

	public class MultiBufferMesh<T0, T1> : Mesh<List<uint>> where T0 : struct, IVertexAttribute where T1 : struct, IVertexAttribute {
		private List<T0> Data0 { get; } = new();
		private List<T1> Data1 { get; } = new();

		public MultiBufferMesh() : base(new()) { }

		protected internal override void BindToBuffer(uint vbo, uint ebo, BufferUsageHint bufferHint) {
			BindArrayBuffer(vbo, Data0.Count * T0.SizeInBytes + Data1.Count * T1.SizeInBytes, bufferHint);

			int bufferOffset = 0;
			byte attribOffset = 0;

			BindSubBuffer(Data0, 0, ref bufferOffset, ref attribOffset);
			BindSubBuffer(Data1, 1, ref bufferOffset, ref attribOffset);

			BindIndices(ebo, bufferHint);
		}

		// TODO mutation methods & check!
	}
}