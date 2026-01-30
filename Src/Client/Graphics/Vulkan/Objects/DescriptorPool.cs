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

		public DescriptorPool(VkDevice logicalDevice, uint poolCount, VkDescriptorType[] descriptorTypes, byte maxFramesInFlight) {
			descriptorPool = CreateDescriptorPool(logicalDevice, poolCount, descriptorTypes, maxFramesInFlight);
			this.logicalDevice = logicalDevice;
			this.maxFramesInFlight = maxFramesInFlight;
		}

		public DescriptorSets AllocateDescriptorSet(VkDescriptorSetLayout descriptorSetLayout) => new(logicalDevice, descriptorPool, descriptorSetLayout, maxFramesInFlight);

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