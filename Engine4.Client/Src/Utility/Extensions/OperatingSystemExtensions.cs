using OpenTK.Graphics.Vulkan;
using OperatingSystem = Engine4.Utility.Compatability.OperatingSystem;

namespace Engine4.Client.Utility.Extensions;

public static class OperatingSystemExtensions {
	extension(OperatingSystem self) {
		public string[] GetRequiredInstanceLayerProperties() =>
				self switch {
						OperatingSystem.Linux => [ ],
						OperatingSystem.Windows => [ ],
						_ => throw new ArgumentOutOfRangeException(nameof(self), self, null),
				};

		public string[] GetRequiredInstanceExtensionProperties() =>
				self switch {
						OperatingSystem.Linux => [ Vk.KhrWaylandSurfaceExtensionName, ], // TODO if wayland. x11 may want it's own extensions
						OperatingSystem.Windows => [ ],
						_ => throw new ArgumentOutOfRangeException(nameof(self), self, null),
				};

		public string[] GetRequiredDeviceExtensionProperties() =>
				self switch {
						OperatingSystem.Linux => [ ],
						OperatingSystem.Windows => [ ],
						_ => throw new ArgumentOutOfRangeException(nameof(self), self, null),
				};
	}
}