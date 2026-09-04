using Engine4.IO;

namespace Engine4;

public class StartupSettings {
	public string MainThreadName { get; init; } = "Main";
	public LoggingSettings LoggingSettings { get; init; } = new();

	public bool LoadVulkan { get; init; }
	public bool LoadGlfw { get; init; }
}