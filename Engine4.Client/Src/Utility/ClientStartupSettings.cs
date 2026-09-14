using Engine4.IO;
using Engine4.Utility;
using NLog;

namespace Engine4.Client.Utility;

public sealed class ClientStartupSettings : StartupSettings {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Engine);

	public VulkanStartupSettings? VulkanSettings { get; init; }

	public bool LoadGlfw { get; init; }

	protected override void PrintValues() {
		base.PrintValues();

		Logger.Debug($"- {nameof(LoadGlfw)}: {LoadGlfw}");
		VulkanSettings?.PrintValues();
	}
}