using JetBrains.Annotations;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public class VulkanBuffer {
	public ulong Size { get; }

	public VulkanBuffer(ulong size) => Size = size;

	public unsafe void Copy(byte* data, ulong dataLength, uint bufferStart = 0, uint dataStart = 0) => throw new NotImplementedException(); // TODO
	public void Copy(byte[] data, uint bufferStart = 0, uint dataStart = 0) => throw new NotImplementedException(); // TODO
	public void Copy(byte[] data, ulong dataLength, uint bufferStart = 0, uint dataStart = 0) => throw new NotImplementedException(); // TODO
	public void Copy(ReadOnlySpan<byte> data, uint bufferStart = 0, uint dataStart = 0) => throw new NotImplementedException(); // TODO
	public void Copy(ReadOnlySpan<byte> data, ulong dataLength, uint bufferStart = 0, uint dataStart = 0) => throw new NotImplementedException(); // TODO

	// map/unmap memory

	[MustUseReturnValue]
	protected bool Validate(ulong dataLength, uint bufferStart, uint dataStart) {
		if (bufferStart + dataLength >= Size) {
			return false; // buffer out of bounds
		} else if (dataStart >= dataLength) {
			return false; // data out of bounds
		}

		return true;
	}
}