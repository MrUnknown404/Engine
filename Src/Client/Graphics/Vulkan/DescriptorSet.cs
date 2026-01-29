using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Exceptions;
using Engine3.Utility;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan {
	public unsafe class DescriptorSet : IDestroyable {
		public bool WasDestroyed { get; private set; }

		private readonly VkDescriptorPool descriptorPool;
		private readonly VkDescriptorSet[] descriptorSets;
		private readonly byte maxFramesInFlight;
		private readonly VkDevice logicalDevice;

		public DescriptorSet(VkDevice logicalDevice, VkDescriptorType[] descriptorSetTypes, VkDescriptorSetLayout descriptorSetLayout, byte maxFramesInFlight) {
			descriptorPool = CreateDescriptorPool(logicalDevice, maxFramesInFlight, descriptorSetTypes);
			descriptorSets = VkH.AllocateDescriptorSets(logicalDevice, descriptorPool, descriptorSetLayout, maxFramesInFlight);
			this.maxFramesInFlight = maxFramesInFlight;
			this.logicalDevice = logicalDevice;
		}

		public VkDescriptorSet GetCurrent(byte currentFrame) => descriptorSets[currentFrame];

		public void UpdateDescriptorSet(uint binding, UniformBuffers uniformBuffers, ulong range, ulong offset = 0) {
			VkWriteDescriptorSet[] writeDescriptorSets = new VkWriteDescriptorSet[maxFramesInFlight];
			VkDescriptorBufferInfo[] bufferInfos = new VkDescriptorBufferInfo[maxFramesInFlight];

			fixed (VkDescriptorBufferInfo* bufferInfosPtr = bufferInfos) {
				for (byte i = 0; i < maxFramesInFlight; i++) {
					bufferInfosPtr[i] = new() { buffer = uniformBuffers.GetBuffer(i), offset = offset, range = range, };
					writeDescriptorSets[i] = new() { dstBinding = binding, dstSet = descriptorSets[i], descriptorType = VkDescriptorType.DescriptorTypeUniformBuffer, descriptorCount = 1, pBufferInfo = &bufferInfosPtr[i], };
				}

				fixed (VkWriteDescriptorSet* writeDescriptorSetsPtr = writeDescriptorSets) { Vk.UpdateDescriptorSets(logicalDevice, (uint)writeDescriptorSets.Length, writeDescriptorSetsPtr, 0, null); }
			}
		}

		public void UpdateDescriptorSet(uint binding, VkImageView imageView, VkSampler textureSampler) {
			VkWriteDescriptorSet[] writeDescriptorSets = new VkWriteDescriptorSet[maxFramesInFlight];
			VkDescriptorImageInfo imageInfo = new() { imageView = imageView, imageLayout = VkImageLayout.ImageLayoutShaderReadOnlyOptimal, sampler = textureSampler, };

			for (int i = 0; i < maxFramesInFlight; i++) {
				writeDescriptorSets[i] = new() { dstBinding = binding, dstSet = descriptorSets[i], descriptorType = VkDescriptorType.DescriptorTypeCombinedImageSampler, descriptorCount = 1, pImageInfo = &imageInfo, };
			}

			fixed (VkWriteDescriptorSet* writeDescriptorSetsPtr = writeDescriptorSets) { Vk.UpdateDescriptorSets(logicalDevice, (uint)writeDescriptorSets.Length, writeDescriptorSetsPtr, 0, null); }
		}

		public void Destroy() {
			if (IDestroyable.WarnIfDestroyed(this)) { return; }

			Vk.DestroyDescriptorPool(logicalDevice, descriptorPool, null);

			WasDestroyed = true;
		}

		[MustUseReturnValue]
		private static VkDescriptorPool CreateDescriptorPool(VkDevice logicalDevice, byte maxFramesInFlight, VkDescriptorType[] descriptorSetTypes) {
			VkDescriptorPoolSize[] poolSizes = new VkDescriptorPoolSize[descriptorSetTypes.Length];
			for (int i = 0; i < poolSizes.Length; i++) { poolSizes[i] = new() { type = descriptorSetTypes[i], descriptorCount = maxFramesInFlight, }; }

			fixed (VkDescriptorPoolSize* poolSizesPtr = poolSizes) {
				VkDescriptorPoolCreateInfo poolCreateInfo = new() { poolSizeCount = (uint)poolSizes.Length, pPoolSizes = poolSizesPtr, maxSets = maxFramesInFlight, };
				VkDescriptorPool descriptorPool;
				VkH.CheckIfSuccess(Vk.CreateDescriptorPool(logicalDevice, &poolCreateInfo, null, &descriptorPool), VulkanException.Reason.CreateDescriptorPool);
				return descriptorPool;
			}
		}
	}
}