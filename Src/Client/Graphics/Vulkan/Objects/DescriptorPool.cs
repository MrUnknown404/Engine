using Engine3.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public sealed unsafe class DescriptorPool : GraphicsResource<DescriptorPool, ulong> {
		public VkDescriptorPool VkDescriptorPool { get; }

		protected override ulong Handle => VkDescriptorPool.Handle;

		private readonly VkDevice logicalDevice;
		private readonly byte maxFramesInFlight;

		internal DescriptorPool(VkDevice logicalDevice, uint poolCount, VkDescriptorType[] descriptorTypes, byte maxFramesInFlight, VkDescriptorPoolCreateFlagBits descriptorPoolCreateFlags) {
			VkDescriptorPool = CreateDescriptorPool(logicalDevice, poolCount, descriptorTypes, maxFramesInFlight, descriptorPoolCreateFlags);
			this.logicalDevice = logicalDevice;
			this.maxFramesInFlight = maxFramesInFlight;

			PrintCreate();
		}

		public DescriptorSets AllocateDescriptorSet(DescriptorSetLayout descriptorSetLayout) => AllocateDescriptorSet(descriptorSetLayout.VkDescriptorSetLayout);

		public DescriptorSets AllocateDescriptorSet(VkDescriptorSetLayout descriptorSetLayout) {
			VkDescriptorSetLayout[] layouts = new VkDescriptorSetLayout[maxFramesInFlight];
			for (int i = 0; i < layouts.Length; i++) { layouts[i] = descriptorSetLayout; }

			VkDescriptorSet[] descriptorSets = new VkDescriptorSet[maxFramesInFlight];
			fixed (VkDescriptorSetLayout* layoutsPtr = layouts) {
				fixed (VkDescriptorSet* descriptorSetsPtr = descriptorSets) {
					VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = new() { descriptorPool = VkDescriptorPool, descriptorSetCount = maxFramesInFlight, pSetLayouts = layoutsPtr, };
					VkH.CheckIfSuccess(Vk.AllocateDescriptorSets(logicalDevice, &descriptorSetAllocateInfo, descriptorSetsPtr), VulkanException.Reason.AllocateDescriptorSets);
					return new(logicalDevice, descriptorSets, maxFramesInFlight);
				}
			}
		}

		protected override void Cleanup() => Vk.DestroyDescriptorPool(logicalDevice, VkDescriptorPool, null);

		[MustUseReturnValue]
		private static VkDescriptorPool CreateDescriptorPool(VkDevice logicalDevice, uint poolCount, VkDescriptorType[] descriptorSetTypes, byte maxFramesInFlight, VkDescriptorPoolCreateFlagBits descriptorPoolCreateFlags) {
			VkDescriptorPoolSize[] poolSizes = new VkDescriptorPoolSize[descriptorSetTypes.Length];
			for (int i = 0; i < poolSizes.Length; i++) { poolSizes[i] = new() { type = descriptorSetTypes[i], descriptorCount = maxFramesInFlight, }; }

			fixed (VkDescriptorPoolSize* poolSizesPtr = poolSizes) {
				VkDescriptorPoolCreateInfo poolCreateInfo = new() { poolSizeCount = (uint)poolSizes.Length, pPoolSizes = poolSizesPtr, maxSets = poolCount * maxFramesInFlight, flags = descriptorPoolCreateFlags, };
				VkDescriptorPool descriptorPool;
				VkH.CheckIfSuccess(Vk.CreateDescriptorPool(logicalDevice, &poolCreateInfo, null, &descriptorPool), VulkanException.Reason.CreateDescriptorPool);
				return descriptorPool;
			}
		}
	}
}