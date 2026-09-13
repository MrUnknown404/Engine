namespace Engine4.Client.Graphics.Vulkan.Objects;

public class VulkanBuffer : IVulkanResource {
	public string DebugName { get; }
	public ulong Size { get; }

	public ulong Handle => throw new NotImplementedException(); // TODO

	public VulkanBuffer(string debugName, ulong size) {
		DebugName = debugName;
		Size = size;
	}

	public unsafe void Copy(byte* data, ulong dataLength, uint bufferStart = 0, uint dataStart = 0) => throw new NotImplementedException(); // TODO
	public void Copy(byte[] data, uint bufferStart = 0, uint dataStart = 0) => throw new NotImplementedException(); // TODO
	public void Copy(byte[] data, ulong dataLength, uint bufferStart = 0, uint dataStart = 0) => throw new NotImplementedException(); // TODO
	public void Copy(ReadOnlySpan<byte> data, uint bufferStart = 0, uint dataStart = 0) => throw new NotImplementedException(); // TODO
	public void Copy(ReadOnlySpan<byte> data, ulong dataLength, uint bufferStart = 0, uint dataStart = 0) => throw new NotImplementedException(); // TODO

	// map/unmap memory
}