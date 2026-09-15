namespace Engine4.Client.Graphics.Vulkan.Resources;

public class VulkanBuffer : VulkanResource { // TODO
	public ulong Size { get; }

	protected override ulong Handle => throw new NotImplementedException();

	public VulkanBuffer(string debugName, ulong size) : base(debugName) => Size = size;

	public unsafe void Copy(byte* data, ulong dataLength, uint bufferStart = 0, uint dataStart = 0) => throw new NotImplementedException(); // TODO
	public void Copy(byte[] data, uint bufferStart = 0, uint dataStart = 0) => throw new NotImplementedException(); // TODO
	public void Copy(byte[] data, ulong dataLength, uint bufferStart = 0, uint dataStart = 0) => throw new NotImplementedException(); // TODO
	public void Copy(ReadOnlySpan<byte> data, uint bufferStart = 0, uint dataStart = 0) => throw new NotImplementedException(); // TODO
	public void Copy(ReadOnlySpan<byte> data, ulong dataLength, uint bufferStart = 0, uint dataStart = 0) => throw new NotImplementedException(); // TODO

	// map/unmap memory

	protected internal override void Cleanup() => throw new NotImplementedException();
}