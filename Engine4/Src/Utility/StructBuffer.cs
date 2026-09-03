using System.Diagnostics.CodeAnalysis;

namespace Engine4.Utility;

public unsafe class StructBuffer<T> where T : unmanaged {
	public required T[] Data { get; init; }

	/// <summary> Element count </summary>
	public uint Count { get; }
	/// <summary> Size in bytes </summary>
	public ulong Size { get; }

	[SetsRequiredMembers]
	public StructBuffer(T[] data) {
		Data = data;
		Count = (uint)data.Length;
		Size = (ulong)sizeof(T) * Count;
	}

	[SetsRequiredMembers]
	public StructBuffer(uint count) {
		Data = new T[count];
		Count = count;
		Size = (ulong)sizeof(T) * Count;
	}
}