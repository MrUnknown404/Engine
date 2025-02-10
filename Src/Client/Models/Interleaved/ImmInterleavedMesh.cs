using JetBrains.Annotations;

namespace USharpLibs.Engine2.Client.Models.Interleaved {
	[PublicAPI]
	public sealed class ImmInterleavedMesh<TVertex> : InterleavedMesh<uint[], TVertex[], TVertex> where TVertex : struct, IInterleavedVertex {
		public ImmInterleavedMesh(TVertex[] vertices, uint[] indices) : base(indices, vertices, false) { }
	}
}