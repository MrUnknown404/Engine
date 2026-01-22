using System.Reflection;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan {
	public class ShaderModule {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VkShaderModule VkShaderModule { get; }
		private readonly VkDevice logicalDevice;
		private bool wasDestroyed;

		public ShaderModule(VkDevice logicalDevice, string fileLocation, ShaderLanguage shaderLang, ShaderType shaderType, Assembly assembly) {
			VkShaderModule = VkH.CreateShaderModule(logicalDevice, fileLocation, shaderLang, shaderType, assembly);
			this.logicalDevice = logicalDevice;
		}

		public unsafe void Destroy() {
			if (wasDestroyed) {
				Logger.Warn($"{nameof(ShaderModule)} was already destroyed");
				return;
			}

			Vk.DestroyShaderModule(logicalDevice, VkShaderModule, null);

			wasDestroyed = true;
		}
	}
}