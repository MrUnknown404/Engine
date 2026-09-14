using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Engine4.Client.Utility;

[PublicAPI]
[MustDisposeResource]
public readonly unsafe ref struct StringArrayUtf8Ptr : IDisposable {
	public IntPtr* Pointer { get; }
	public uint Length { get; }

	public StringArrayUtf8Ptr(string[] str) {
		Pointer = (IntPtr*)Marshal.AllocCoTaskMem(str.Length * sizeof(IntPtr));
		Length = (uint)str.Length;

		for (int i = 0; i < str.Length; i++) { Pointer[i] = Marshal.StringToCoTaskMemUTF8(str[i]); }
	}

	public void Dispose() {
		for (int i = 0; i < Length; i++) { Marshal.FreeCoTaskMem(Pointer[i]); }
		Marshal.FreeCoTaskMem((IntPtr)Pointer);
	}

	public static implicit operator byte**(StringArrayUtf8Ptr self) => (byte**)self.Pointer;
}