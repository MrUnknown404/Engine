namespace Engine4.Client.Graphics.Vulkan.Objects;

public class VulkanBuffer : GraphicsBuffer {
	public VulkanBuffer(ulong size) : base(size) { }

	public override unsafe void Copy(byte* data, ulong dataLength, uint bufferStart = 0, uint dataStart = 0) => throw new NotImplementedException(); // TODO
	public override void Copy(byte[] data, uint bufferStart = 0, uint dataStart = 0) => throw new NotImplementedException(); // TODO
	public override void Copy(byte[] data, ulong dataLength, uint bufferStart = 0, uint dataStart = 0) => throw new NotImplementedException(); // TODO
	public override void Copy(ReadOnlySpan<byte> data, uint bufferStart = 0, uint dataStart = 0) => throw new NotImplementedException(); // TODO
	public override void Copy(ReadOnlySpan<byte> data, ulong dataLength, uint bufferStart = 0, uint dataStart = 0) => throw new NotImplementedException(); // TODO

	// map/unmap memory
}