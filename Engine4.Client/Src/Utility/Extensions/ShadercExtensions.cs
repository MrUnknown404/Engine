using JetBrains.Annotations;
using Silk.NET.Shaderc;

namespace Engine4.Client.Utility.Extensions;

public static unsafe class ShadercExtensions {
	extension(Shaderc self) {
		[Pure]
		public void GetSpvVersion(out uint version, out uint revision, out byte major, out byte minor) {
			fixed (uint* pVersion = &version) {
				fixed (uint* pRevision = &revision) { self.GetSpvVersion(pVersion, pRevision); }
			}

			major = (byte)((version >> 16) & 0xFF);
			minor = (byte)((version >> 8) & 0xFF);
		}
	}
}