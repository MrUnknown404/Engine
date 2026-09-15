using Engine4.Utility.Math;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Utility.Extensions;

public static class Color3Extensions {
	extension(Color3 self) {
		public unsafe VkClearColorValue ToVkClearColorValue() {
			VkClearColorValue clearColorValue = new();
			clearColorValue.float32[0] = self.R;
			clearColorValue.float32[1] = self.G;
			clearColorValue.float32[2] = self.B;
			clearColorValue.float32[3] = 1;
			return clearColorValue;
		}
	}
}