namespace USharpLibs.Engine2.Client.Models.Separated {
	public sealed class MutSeparateMesh<T0> : SeparateMesh<List<uint>, T0> where T0 : struct, IVertexAttribute {
		public MutSeparateMesh() : base(new(), new List<T0>(), true) { }

		// TODO mutable methods
	}

	public sealed class MutSeparateMesh<T0, T1> : SeparateMesh<List<uint>, T0, T1> where T0 : struct, IVertexAttribute where T1 : struct, IVertexAttribute {
		public MutSeparateMesh() : base(new(), new List<T0>(), new List<T1>(), true) { }

		// TODO mutable methods
	}
}