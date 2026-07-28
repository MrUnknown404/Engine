namespace Engine4.Graphics;

public abstract class GpuBuffer {
	public ulong Size { get; }

	internal GpuBuffer(ulong size) => Size = size;

	public abstract unsafe void Copy(byte* data, ulong start, uint offset = 0);
	public abstract void Copy(byte[] data, ulong start, uint offset = 0);
	public abstract void Copy(ReadOnlySpan<byte> data, ulong start, uint offset = 0);
}