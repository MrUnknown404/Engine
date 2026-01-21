using OpenTK.Graphics.Vulkan;
using OpenTK.Mathematics;

namespace Engine3.Utils.Extensions {
	public static class Color4Extensions {
		extension(Color4<Rgba> self) {
			public unsafe VkClearColorValue ToVkClearColorValue() {
				VkClearColorValue clearColorValue = new();
				clearColorValue.float32[0] = self.X;
				clearColorValue.float32[1] = self.Y;
				clearColorValue.float32[2] = self.Z;
				clearColorValue.float32[3] = self.W;
				return clearColorValue;
			}
		}
	}
}