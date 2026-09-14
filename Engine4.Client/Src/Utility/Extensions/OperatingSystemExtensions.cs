using OpenTK.Graphics.Vulkan;
using OperatingSystem = Engine4.Utility.Compatability.OperatingSystem;

namespace Engine4.Client.Utility.Extensions;

public static class OperatingSystemExtensions {
	extension(OperatingSystem self) {
		public string[] GetRequiredEngineInstanceLayerProperties() =>
				self switch {
						OperatingSystem.Linux => [ ],
						OperatingSystem.Windows => [ ],
						_ => throw new ArgumentOutOfRangeException(nameof(self), self, null),
				};

		public string[] GetRequiredEngineInstanceExtensionProperties() =>
				self switch {
						OperatingSystem.Linux => [ Vk.KhrWaylandSurfaceExtensionName, ],
						OperatingSystem.Windows => [ ],
						_ => throw new ArgumentOutOfRangeException(nameof(self), self, null),
				};

		public string[] GetRequiredEngineDeviceExtensionProperties() =>
				self switch {
						OperatingSystem.Linux => [ ],
						OperatingSystem.Windows => [ ],
						_ => throw new ArgumentOutOfRangeException(nameof(self), self, null),
				};
	}
}