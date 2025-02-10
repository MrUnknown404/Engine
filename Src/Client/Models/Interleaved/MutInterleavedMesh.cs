using JetBrains.Annotations;

namespace USharpLibs.Engine2.Client.Models.Interleaved {
	[PublicAPI]
	public sealed class MutInterleavedMesh<TVertex> : InterleavedMesh<List<uint>, List<TVertex>, TVertex> where TVertex : struct, IInterleavedVertex {
		public MutInterleavedMesh() : base(new(), new(), true) { }

		// TODO mutable methods
	}
}