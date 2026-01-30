using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public unsafe class DepthImage : IGraphicsResource {
		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		public VkImage Image { get; private set; } // TODO replace with VkImageObject when i can
		public VkDeviceMemory ImageMemory { get; private set; }
		public VkImageView ImageView { get; private set; }

		private readonly VkPhysicalDevice physicalDevice;
		private readonly VkDevice logicalDevice;
		private readonly QueueFamilyIndices queueFamilyIndices;
		private readonly VkPhysicalDeviceMemoryProperties memoryProperties;
		private readonly VkCommandPool transferCommandPool;
		private readonly VkQueue transferQueue;

		public DepthImage(string debugName, VkPhysicalDevice physicalDevice, VkDevice logicalDevice, QueueFamilyIndices queueFamilyIndices, VkPhysicalDeviceMemoryProperties memoryProperties, VkCommandPool transferCommandPool,
			VkQueue transferQueue, VkExtent2D extent) {
			DebugName = debugName;
			this.physicalDevice = physicalDevice;
			this.logicalDevice = logicalDevice;
			this.queueFamilyIndices = queueFamilyIndices;
			this.memoryProperties = memoryProperties;
			this.transferCommandPool = transferCommandPool;
			this.transferQueue = transferQueue;

			Recreate(extent);
		}

		public void Recreate(VkExtent2D extent) {
			Destroy();

			VkFormat depthFormat = VkH.FindDepthFormat(physicalDevice);

			Image = VkH.CreateImage(logicalDevice, depthFormat, VkImageTiling.ImageTilingOptimal, VkImageUsageFlagBits.ImageUsageDepthStencilAttachmentBit, extent.width, extent.height);
			ImageMemory = VkH.CreateDeviceMemory(memoryProperties, logicalDevice, Image, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit);
			VkH.BindImageMemory(logicalDevice, Image, ImageMemory);

			ImageView = VkH.CreateImageView(logicalDevice, Image, depthFormat, VkImageAspectFlagBits.ImageAspectDepthBit);

			TransferCommandBufferObject transferCommandBuffer = new(logicalDevice, transferCommandPool, transferQueue);
			transferCommandBuffer.BeginCommandBuffer(VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit);

			transferCommandBuffer.TransitionImageLayout(queueFamilyIndices, Image, depthFormat, VkImageLayout.ImageLayoutUndefined, VkImageLayout.ImageLayoutDepthStencilAttachmentOptimal);

			transferCommandBuffer.EndCommandBuffer();
			transferCommandBuffer.SubmitQueue();
			Vk.QueueWaitIdle(transferQueue);

			transferCommandBuffer.Destroy();

			WasDestroyed = false;
		}

		public void Destroy() {
			if (IGraphicsResource.WarnIfDestroyed(this)) { return; }

			Vk.DestroyImageView(logicalDevice, ImageView, null);
			Vk.DestroyImage(logicalDevice, Image, null);
			Vk.FreeMemory(logicalDevice, ImageMemory, null);

			WasDestroyed = true;
		}
	}
}