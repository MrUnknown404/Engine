namespace USharpLibs.Engine2.Client.Models.Separated {
	public sealed class ImmSeparateMesh<T0> : SeparateMesh<uint[], T0> where T0 : struct, IVertexAttribute {
		public ImmSeparateMesh(uint[] indices, IList<T0> data0) : base(indices, data0, false) { }
	}

	public sealed class ImmSeparateMesh<T0, T1> : SeparateMesh<uint[], T0, T1> where T0 : struct, IVertexAttribute where T1 : struct, IVertexAttribute {
		public ImmSeparateMesh(uint[] indices, IList<T0> data0, IList<T1> data1) : base(indices, data0, data1, false) { }
	}
}