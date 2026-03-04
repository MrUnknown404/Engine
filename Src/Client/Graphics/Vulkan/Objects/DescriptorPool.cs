using Engine3.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public sealed unsafe class DescriptorPool : GraphicsResource<DescriptorPool, ulong> {
		public VkDescriptorPool VkDescriptorPool { get; }

		protected override ulong Handle => VkDescriptorPool.Handle;

		private readonly LogicalGpu logicalGpu;
		private readonly byte maxFramesInFlight;

		internal DescriptorPool(LogicalGpu logicalGpu, uint poolCount, VkDescriptorType[] descriptorTypes, byte maxFramesInFlight, VkDescriptorPoolCreateFlagBits descriptorPoolCreateFlags) {
			VkDescriptorPool = CreateDescriptorPool(logicalGpu, poolCount, descriptorTypes, maxFramesInFlight, descriptorPoolCreateFlags);
			this.logicalGpu = logicalGpu;
			this.maxFramesInFlight = maxFramesInFlight;

			PrintCreate();
		}

		public DescriptorSets AllocateDescriptorSets(DescriptorSetLayout descriptorSetLayout) {
			VkDescriptorSetLayout* layouts = stackalloc VkDescriptorSetLayout[maxFramesInFlight];
			for (int i = 0; i < maxFramesInFlight; i++) { layouts[i] = descriptorSetLayout.VkDescriptorSetLayout; }

			VkDescriptorSet[] descriptorSets = new VkDescriptorSet[maxFramesInFlight];

			fixed (VkDescriptorSet* descriptorSetsPtr = descriptorSets) {
				VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = new() { descriptorPool = VkDescriptorPool, descriptorSetCount = maxFramesInFlight, pSetLayouts = layouts, };
				VkH.CheckIfSuccess(Vk.AllocateDescriptorSets(logicalGpu.LogicalDevice, &descriptorSetAllocateInfo, descriptorSetsPtr), VulkanException.Reason.AllocateDescriptorSets);
				return new(logicalGpu, descriptorSets, maxFramesInFlight);
			}
		}

		public DescriptorSet AllocateDescriptorSet(DescriptorSetLayout descriptorSetLayout) {
			VkDescriptorSetLayout layout = descriptorSetLayout.VkDescriptorSetLayout;
			VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = new() { descriptorPool = VkDescriptorPool, descriptorSetCount = 1, pSetLayouts = &layout, };

			VkDescriptorSet descriptorSet;
			VkH.CheckIfSuccess(Vk.AllocateDescriptorSets(logicalGpu.LogicalDevice, &descriptorSetAllocateInfo, &descriptorSet), VulkanException.Reason.AllocateDescriptorSets);
			return new(logicalGpu, descriptorSet);
		}

		protected override void Cleanup() => Vk.DestroyDescriptorPool(logicalGpu.LogicalDevice, VkDescriptorPool, null);

		[MustUseReturnValue]
		private static VkDescriptorPool CreateDescriptorPool(LogicalGpu logicalGpu, uint poolCount, VkDescriptorType[] descriptorSetTypes, byte maxFramesInFlight, VkDescriptorPoolCreateFlagBits descriptorPoolCreateFlags) {
			int length = descriptorSetTypes.Length;

			VkDescriptorPoolSize* poolSizes = stackalloc VkDescriptorPoolSize[length];
			for (int i = 0; i < length; i++) { poolSizes[i] = new() { type = descriptorSetTypes[i], descriptorCount = maxFramesInFlight, }; }

			VkDescriptorPoolCreateInfo poolCreateInfo = new() { poolSizeCount = (uint)length, pPoolSizes = poolSizes, maxSets = poolCount * maxFramesInFlight, flags = descriptorPoolCreateFlags, };
			VkDescriptorPool descriptorPool;
			VkH.CheckIfSuccess(Vk.CreateDescriptorPool(logicalGpu.LogicalDevice, &poolCreateInfo, null, &descriptorPool), VulkanException.Reason.CreateDescriptorPool);
			return descriptorPool;
		}
	}
}