using System.Diagnostics.CodeAnalysis;

namespace Engine3.Client.Graphics {
	public unsafe class StructBuffer<T> where T : unmanaged {
		public static byte StructByteSize { get; } = (byte)sizeof(T);

		public required T[] Data { get; init; }

		/// <summary> Element count </summary>
		public uint Count { get; }
		/// <summary> Size in bytes </summary>
		public ulong Size { get; }

		[SetsRequiredMembers]
		public StructBuffer(T[] data) {
			Data = data;
			Count = (uint)data.Length;
			Size = StructByteSize * Count;
		}

		[SetsRequiredMembers]
		public StructBuffer(uint count) {
			Data = new T[count];
			Count = count;
			Size = StructByteSize * Count;
		}
	}
}