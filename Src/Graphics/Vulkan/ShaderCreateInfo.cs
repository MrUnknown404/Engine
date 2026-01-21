using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan {
	public readonly record struct ShaderCreateInfo {
		public required VkShaderModule ShaderModule { get; init; }
		public required VkShaderStageFlagBits ShaderStageFlags { get; init; }

		private readonly VkDevice logicalDevice;

		public ShaderCreateInfo(VkDevice logicalDevice) => this.logicalDevice = logicalDevice;

		[SetsRequiredMembers]
		public ShaderCreateInfo(VkDevice logicalDevice, VkShaderModule shaderModule, VkShaderStageFlagBits shaderStageFlags) {
			this.logicalDevice = logicalDevice;
			ShaderModule = shaderModule;
			ShaderStageFlags = shaderStageFlags;
		}

		public unsafe void Cleanup() => Vk.DestroyShaderModule(logicalDevice, ShaderModule, null);
	}
}