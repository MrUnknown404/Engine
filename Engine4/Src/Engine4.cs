using Engine4.Utility.Versions;

namespace Engine4;

public static class Engine4 {
	public const string Name = nameof(Engine4);
	public const bool Debug =
#if DEBUG
			true;
#else
			false;
#endif

	public static readonly IPackableVersion Version = new BuildVersion(0);
}