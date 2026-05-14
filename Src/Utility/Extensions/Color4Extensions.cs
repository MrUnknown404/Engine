using OpenTK.Graphics.Vulkan;
using OpenTK.Mathematics;
using Vector4 = System.Numerics.Vector4;

namespace Engine3.Utility.Extensions;

public static class Color4Extensions {
	extension<T>(Color4<T> self) where T : IColorSpace4 {
		public unsafe VkClearColorValue ToVkClearColorValue() {
			VkClearColorValue clearColorValue = new();
			clearColorValue.float32[0] = self.X;
			clearColorValue.float32[1] = self.Y;
			clearColorValue.float32[2] = self.Z;
			clearColorValue.float32[3] = self.W;
			return clearColorValue;
		}

		public static Color4<T> FromVector4(Vector4 vector4) => new(vector4.X, vector4.Y, vector4.Z, vector4.W);
	}
}