using System.Diagnostics.CodeAnalysis;

namespace Engine4.Client.Graphics._Test;

public readonly record struct DebugHandle { // TODO this was for testing. but use something similar?
	public required string DebugName { get; init; }
	public required ulong Handle { get; init; }

	[SetsRequiredMembers]
	public DebugHandle(string debugName, ulong handle) {
		DebugName = debugName;
		Handle = handle;
	}
}