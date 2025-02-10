using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine2.Debug;

namespace USharpLibs.Engine2.Client.Models.Separated {
	public abstract class SeparateMesh<TIndex> : BaseMesh<TIndex>, ISeparateMesh where TIndex : IList<uint> {
		internal SeparateMesh(TIndex indices, bool allowEmptyData) : base(indices, allowEmptyData) { }

		protected void BindSubBuffer<T>(IList<T> data, byte attribIndex, ref int bufferOffset, ref byte attribOffset) where T : struct, IVertexAttribute {
			int size = data.Count * T.SizeInBytes;
			GL.BufferSubData(BufferTarget.ArrayBuffer, bufferOffset, size, CollectVertexData(data));
			BindVertexAttrib(attribIndex, T.VertexLayout, T.SizeInBytes, ref attribOffset);
		}
	}

	public abstract class SeparateMesh<TIndex, T0> : SeparateMesh<TIndex> where TIndex : IList<uint> where T0 : struct, IVertexAttribute {
		private IList<T0> Data0 { get; }

		internal SeparateMesh(TIndex indices, IList<T0> data0, bool allowEmptyData) : base(indices, allowEmptyData) {
			bool _ = MeshErrorHandler.Assert(!allowEmptyData && data0.Count == 0, static () => new(MeshErrorHandler.Reason.EmptyVertexArray));

			Data0 = data0;
		}

		public override void BindToBuffer(uint vbo, uint ebo, BufferUsageHint bufferHint) {
			BindArrayBuffer(vbo, Data0.Count * T0.SizeInBytes, bufferHint);

			int bufferOffset = 0;
			byte attribOffset = 0;

			BindSubBuffer(Data0, 0, ref bufferOffset, ref attribOffset);

			BindIndices(ebo, bufferHint);
		}
	}

	public abstract class SeparateMesh<TIndex, T0, T1> : SeparateMesh<TIndex> where TIndex : IList<uint> where T0 : struct, IVertexAttribute where T1 : struct, IVertexAttribute {
		private IList<T0> Data0 { get; }
		private IList<T1> Data1 { get; }

		internal SeparateMesh(TIndex indices, IList<T0> data0, IList<T1> data1, bool allowEmptyData) : base(indices, allowEmptyData) {
			_ = MeshErrorHandler.Assert(!allowEmptyData && (data0.Count == 0 || data1.Count == 0), static () => new(MeshErrorHandler.Reason.EmptyVertexArray));

			int length = data0.Count;

			_ = MeshErrorHandler.Assert(data1.Count != length, static () => new(MeshErrorHandler.Reason.IncorrectlySizedIndexArray));

			Data0 = data0;
			Data1 = data1;
		}

		public override void BindToBuffer(uint vbo, uint ebo, BufferUsageHint bufferHint) {
			BindArrayBuffer(vbo, Data0.Count * T0.SizeInBytes + Data1.Count * T1.SizeInBytes, bufferHint);

			int bufferOffset = 0;
			byte attribOffset = 0;

			BindSubBuffer(Data0, 0, ref bufferOffset, ref attribOffset);
			BindSubBuffer(Data1, 1, ref bufferOffset, ref attribOffset);

			BindIndices(ebo, bufferHint);
		}
	}
}