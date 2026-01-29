using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan {
	public readonly record struct DescriptorSetInfo {
		public required VkDescriptorType DescriptorType { get; init; }
		public required VkShaderStageFlagBits StageFlags { get; init; }
		public required uint BindingLocation { get; init; }

		[SetsRequiredMembers]
		public DescriptorSetInfo(VkDescriptorType descriptorType, VkShaderStageFlagBits stageFlags, uint bindingLocation) {
			DescriptorType = descriptorType;
			StageFlags = stageFlags;
			BindingLocation = bindingLocation;
		}
	}
}