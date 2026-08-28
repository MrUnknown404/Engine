using JetBrains.Annotations;

namespace Engine4.Client.Graphics;

public abstract class GraphicsBuffer {
	public ulong Size { get; }

	internal GraphicsBuffer(ulong size) => Size = size;

	public abstract unsafe void Copy(byte* data, ulong dataLength, uint bufferStart = 0, uint dataStart = 0);
	public abstract void Copy(byte[] data, uint bufferStart = 0, uint dataStart = 0);
	public abstract void Copy(byte[] data, ulong dataLength, uint bufferStart = 0, uint dataStart = 0);
	public abstract void Copy(ReadOnlySpan<byte> data, uint bufferStart = 0, uint dataStart = 0);
	public abstract void Copy(ReadOnlySpan<byte> data, ulong dataLength, uint bufferStart = 0, uint dataStart = 0);

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