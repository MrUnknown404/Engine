using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

[Obsolete]
public unsafe class DescriptorSetLayout : VulkanResource {
	public VkDescriptorSetLayout VkDescriptorSetLayout { get; }

	public override ulong Handle => VkDescriptorSetLayout.Handle;

	private readonly LogicalGpu logicalGpu;

	internal DescriptorSetLayout(string debugName, LogicalGpu logicalGpu, DescriptorSetInfo[] descriptorSets) : base(debugName) {
		this.logicalGpu = logicalGpu;
		VkDescriptorSetLayout = CreateDescriptorSetLayout(logicalGpu, descriptorSets);
	}

	protected internal override void Cleanup() => Vk.DestroyDescriptorSetLayout(logicalGpu.VkLogicalDevice, VkDescriptorSetLayout, null);

	[MustUseReturnValue]
	private static VkDescriptorSetLayout CreateDescriptorSetLayout(LogicalGpu logicalGpu, DescriptorSetInfo[] descriptorSets) {
		VkDescriptorSetLayoutBinding[] descriptorSetLayoutBindings = descriptorSets.Select(static info => new VkDescriptorSetLayoutBinding {
				binding = info.BindingLocation, descriptorType = info.DescriptorType, stageFlags = info.StageFlags, descriptorCount = 1,
		}).ToArray();

		fixed (VkDescriptorSetLayoutBinding* pDescriptorSetLayoutBindings = descriptorSetLayoutBindings) {
			VkDescriptorSetLayoutCreateInfo layoutCreateInfo = new() { bindingCount = (uint)descriptorSetLayoutBindings.Length, pBindings = pDescriptorSetLayoutBindings, };
			VkDescriptorSetLayout descriptorSetLayout;
			VkH.ThrowIfNotSuccess(Vk.CreateDescriptorSetLayout(logicalGpu.VkLogicalDevice, &layoutCreateInfo, null, &descriptorSetLayout), "Failed to create descriptor set layout");
			return descriptorSetLayout;
		}
	}
}