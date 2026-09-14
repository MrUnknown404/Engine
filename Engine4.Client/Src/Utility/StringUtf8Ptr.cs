using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Engine4.Client.Utility;

[MustDisposeResource]
public readonly unsafe record struct StringUtf8Ptr : IDisposable {
	public IntPtr Pointer { get; }

	public StringUtf8Ptr(string str) => Pointer = Marshal.StringToCoTaskMemUTF8(str);

	public void Dispose() => Marshal.FreeCoTaskMem(Pointer);

	public static implicit operator byte*(StringUtf8Ptr self) => (byte*)self.Pointer;
}