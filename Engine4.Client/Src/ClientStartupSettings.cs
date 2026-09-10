namespace Engine4.Client;

public class ClientStartupSettings : StartupSettings {
	public bool LoadVulkan { get; init; }
	public bool LoadGlfw { get; init; }
}