using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

[Obsolete]
public unsafe class DescriptorPool : VulkanResource {
	public VkDescriptorPool VkDescriptorPool { get; }

	public override ulong Handle => VkDescriptorPool.Handle;

	private readonly LogicalGpu logicalGpu;
	private readonly byte maxFramesInFlight;

	internal DescriptorPool(LogicalGpu logicalGpu, uint poolCount, VkDescriptorType[] descriptorTypes, byte maxFramesInFlight, VkDescriptorPoolCreateFlagBits descriptorPoolCreateFlags) {
		VkDescriptorPool = CreateDescriptorPool(logicalGpu, poolCount, descriptorTypes, maxFramesInFlight, descriptorPoolCreateFlags);
		this.logicalGpu = logicalGpu;
		this.maxFramesInFlight = maxFramesInFlight;
	}

	public DescriptorSets AllocateDescriptorSets(DescriptorSetLayout descriptorSetLayout) {
		VkDescriptorSetLayout* layouts = stackalloc VkDescriptorSetLayout[maxFramesInFlight];
		for (int i = 0; i < maxFramesInFlight; i++) { layouts[i] = descriptorSetLayout.VkDescriptorSetLayout; }

		VkDescriptorSet[] descriptorSets = new VkDescriptorSet[maxFramesInFlight];

		fixed (VkDescriptorSet* pDescriptorSets = descriptorSets) {
			VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = new() { descriptorPool = VkDescriptorPool, descriptorSetCount = maxFramesInFlight, pSetLayouts = layouts, };
			VkH.ThrowIfNotSuccess(Vk.AllocateDescriptorSets(logicalGpu.VkLogicalDevice, &descriptorSetAllocateInfo, pDescriptorSets), "Failed to allocate descriptor sets");
			return new(logicalGpu, descriptorSets, maxFramesInFlight);
		}
	}

	public DescriptorSet AllocateDescriptorSet(DescriptorSetLayout descriptorSetLayout) {
		VkDescriptorSetLayout layout = descriptorSetLayout.VkDescriptorSetLayout;
		VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = new() { descriptorPool = VkDescriptorPool, descriptorSetCount = 1, pSetLayouts = &layout, };

		VkDescriptorSet descriptorSet;
		VkH.ThrowIfNotSuccess(Vk.AllocateDescriptorSets(logicalGpu.VkLogicalDevice, &descriptorSetAllocateInfo, &descriptorSet), "Failed to allocate descriptor sets");
		return new(logicalGpu, descriptorSet);
	}

	protected internal override void Cleanup() => Vk.DestroyDescriptorPool(logicalGpu.VkLogicalDevice, VkDescriptorPool, null);

	[MustUseReturnValue]
	private static VkDescriptorPool CreateDescriptorPool(LogicalGpu logicalGpu, uint poolCount, VkDescriptorType[] descriptorSetTypes, byte maxFramesInFlight, VkDescriptorPoolCreateFlagBits descriptorPoolCreateFlags) {
		int length = descriptorSetTypes.Length;

		VkDescriptorPoolSize* poolSizes = stackalloc VkDescriptorPoolSize[length];
		for (int i = 0; i < length; i++) { poolSizes[i] = new() { type = descriptorSetTypes[i], descriptorCount = maxFramesInFlight, }; }

		VkDescriptorPoolCreateInfo poolCreateInfo = new() { poolSizeCount = (uint)length, pPoolSizes = poolSizes, maxSets = poolCount * maxFramesInFlight, flags = descriptorPoolCreateFlags, };
		VkDescriptorPool descriptorPool;
		VkH.ThrowIfNotSuccess(Vk.CreateDescriptorPool(logicalGpu.VkLogicalDevice, &poolCreateInfo, null, &descriptorPool), "Failed to create descriptor pool");
		return descriptorPool;
	}
}