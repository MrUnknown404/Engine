using Engine3.Exceptions;
using Engine3.Utility;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public unsafe class DescriptorPool : IDestroyable {
		private readonly VkDescriptorPool descriptorPool;
		private readonly VkDevice logicalDevice;
		private readonly byte maxFramesInFlight;

		public bool WasDestroyed { get; private set; }

		internal DescriptorPool(VkDevice logicalDevice, uint poolCount, VkDescriptorType[] descriptorTypes, byte maxFramesInFlight) {
			descriptorPool = CreateDescriptorPool(logicalDevice, poolCount, descriptorTypes, maxFramesInFlight);
			this.logicalDevice = logicalDevice;
			this.maxFramesInFlight = maxFramesInFlight;
		}

		public DescriptorSets AllocateDescriptorSet(VkDescriptorSetLayout descriptorSetLayout) {
			VkDescriptorSetLayout[] layouts = new VkDescriptorSetLayout[maxFramesInFlight];
			for (int i = 0; i < layouts.Length; i++) { layouts[i] = descriptorSetLayout; }

			VkDescriptorSet[] descriptorSets = new VkDescriptorSet[maxFramesInFlight];
			fixed (VkDescriptorSetLayout* layoutsPtr = layouts) {
				fixed (VkDescriptorSet* descriptorSetsPtr = descriptorSets) {
					VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = new() { descriptorPool = descriptorPool, descriptorSetCount = maxFramesInFlight, pSetLayouts = layoutsPtr, };
					VkH.CheckIfSuccess(Vk.AllocateDescriptorSets(logicalDevice, &descriptorSetAllocateInfo, descriptorSetsPtr), VulkanException.Reason.AllocateDescriptorSets);
					return new(logicalDevice, descriptorSets, maxFramesInFlight);
				}
			}
		}

		public void Destroy() {
			if (IDestroyable.WarnIfDestroyed(this)) { return; }

			Vk.DestroyDescriptorPool(logicalDevice, descriptorPool, null);

			WasDestroyed = true;
		}

		[MustUseReturnValue]
		private static VkDescriptorPool CreateDescriptorPool(VkDevice logicalDevice, uint poolCount, VkDescriptorType[] descriptorSetTypes, byte maxFramesInFlight) {
			VkDescriptorPoolSize[] poolSizes = new VkDescriptorPoolSize[descriptorSetTypes.Length];
			for (int i = 0; i < poolSizes.Length; i++) { poolSizes[i] = new() { type = descriptorSetTypes[i], descriptorCount = maxFramesInFlight, }; }

			fixed (VkDescriptorPoolSize* poolSizesPtr = poolSizes) {
				VkDescriptorPoolCreateInfo poolCreateInfo = new() { poolSizeCount = (uint)poolSizes.Length, pPoolSizes = poolSizesPtr, maxSets = poolCount, };
				VkDescriptorPool descriptorPool;
				VkH.CheckIfSuccess(Vk.CreateDescriptorPool(logicalDevice, &poolCreateInfo, null, &descriptorPool), VulkanException.Reason.CreateDescriptorPool);
				return descriptorPool;
			}
		}
	}
}