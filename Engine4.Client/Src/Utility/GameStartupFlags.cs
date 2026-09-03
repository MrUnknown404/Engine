namespace Engine4.Client.Utility;

[Flags]
public enum GameStartupFlags : byte {
	None = 0,

	/// <summary> Enabled the use of GLFW Windows &amp; Events </summary>
	UseGlfw = 1 << 0,
	/// <summary> Enabled the use of Vulkan </summary>
	UseVulkan = 1 << 1,

	All = byte.MaxValue,
}

public static class GameStartupFlagsExtensions {
	extension(GameStartupFlags value) {
		public bool HasFlagFast(GameStartupFlags flag) => (value & flag) != 0;
	}
}