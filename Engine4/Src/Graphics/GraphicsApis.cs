namespace Engine4.Graphics;

[Flags]
public enum GraphicsApis : byte { // what should this be called?
	None = 0,
	OpenGL = 1 << 0,
	Vulkan = 1 << 1,
	Software = 1 << 2,
	All = OpenGL | Vulkan | Software,
}

public static class GraphicsApisExtensions {
	extension(GraphicsApis value) {
		public bool HasFlagFast(GraphicsApis flag) => (value & flag) != 0;
	}
}