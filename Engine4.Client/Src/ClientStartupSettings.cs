using Engine4.IO;
using NLog;

namespace Engine4.Client;

public sealed class ClientStartupSettings : StartupSettings {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Engine);

	public bool LoadVulkan { get; init; }
	public bool LoadGlfw { get; init; }

	protected override void PrintValues() {
		base.PrintValues();

		Logger.Debug($"- {nameof(LoadVulkan)}: {LoadVulkan}");
		Logger.Debug($"- {nameof(LoadGlfw)}: {LoadGlfw}");
	}
}