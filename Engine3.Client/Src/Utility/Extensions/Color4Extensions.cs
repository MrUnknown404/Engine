using OpenTK.Graphics.Vulkan;
using OpenTK.Mathematics;
using Color4 = Engine3.Core.Utility.Color4;

namespace Engine3.Client.Utility.Extensions;

public static class Color4Extensions {
	extension(Color4 self) {
		public unsafe VkClearColorValue ToVkClearColorValue() {
			VkClearColorValue clearColorValue = new();
			clearColorValue.float32[0] = self.R;
			clearColorValue.float32[1] = self.G;
			clearColorValue.float32[2] = self.B;
			clearColorValue.float32[3] = self.A;
			return clearColorValue;
		}

		public Color4<Rgba> ToOpenTKColor4() => new(self.R, self.G, self.B, self.A);
	}
}