namespace Engine4.Graphics.Software;

public class SoftwareBuffer : GpuBuffer {
	public SoftwareBuffer(ulong size) : base(size) { }

	public override unsafe void Copy(byte* data, ulong start, uint offset = 0) => throw new NotImplementedException(); // TODO
	public override void Copy(byte[] data, ulong start, uint offset = 0) => throw new NotImplementedException(); // TODO
	public override void Copy(ReadOnlySpan<byte> data, ulong start, uint offset = 0) => throw new NotImplementedException(); // TODO
}