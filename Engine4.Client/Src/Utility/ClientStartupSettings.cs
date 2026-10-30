using Engine4.Client.Graphics.Vulkan;
using Engine4.IO;
using Engine4.Utility;
using NLog;

namespace Engine4.Client.Utility;

public sealed class ClientStartupSettings : StartupSettings {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Engine);

	/// <summary> Enables the use of Windows </summary>
	public bool EnableGlfw { get; init; }
	/// <summary> Enables the use of Vulkan. Set by setting <see cref="VulkanSettings"/> to not null </summary>
	public bool EnableVulkan { get; private init; }

	public SetupVulkanSettings? VulkanSettings {
		get;
		init {
			field = value;
			EnableVulkan = field != null;
		}
	}

	protected override void PrintValues() {
		base.PrintValues(); //

		Logger.Trace($"- {nameof(EnableGlfw)}: {EnableGlfw}");
		Logger.Trace($"- {nameof(EnableVulkan)}: {EnableVulkan}");

		VulkanSettings?.PrintValues();
	}
}