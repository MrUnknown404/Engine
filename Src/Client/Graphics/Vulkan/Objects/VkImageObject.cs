using System.Reflection;
using Engine3.Utility;
using OpenTK.Graphics.Vulkan;
using StbiSharp;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public unsafe class VkImageObject : IGraphicsResource {
		public VkImage Image { get; }
		public VkDeviceMemory ImageMemory { get; }
		public VkImageView ImageView { get; }
		public VkFormat ImageFormat { get; }

		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		private readonly VkDevice logicalDevice;

		public VkImageObject(string debugName, VkPhysicalDeviceMemoryProperties memoryProperties, VkDevice logicalDevice, VkCommandPool transferCommandPool, VkQueue transferQueue, QueueFamilyIndices queueFamilyIndices,
			string fileLocation, string fileExtension, byte texChannels, VkFormat imageFormat, Assembly assembly) {
			DebugName = debugName;
			ImageFormat = imageFormat;
			this.logicalDevice = logicalDevice;

			// TODO allow loading of data later
			using (StbiImage stbiImage = AssetH.LoadImage(fileLocation, fileExtension, texChannels, assembly)) {
				CreateTexture(memoryProperties, logicalDevice, transferCommandPool, transferQueue, queueFamilyIndices, stbiImage, texChannels, imageFormat, out VkImage image, out VkDeviceMemory imageMemory);
				Image = image;
				ImageMemory = imageMemory;
				ImageView = VkH.CreateImageView(logicalDevice, Image, imageFormat, VkImageAspectFlagBits.ImageAspectColorBit);
			}
		}

		public static VkImageObject CreateFromRgbaPng(string debugName, VkPhysicalDeviceMemoryProperties memoryProperties, VkDevice logicalDevice, VkCommandPool transferCommandPool, VkQueue transferQueue,
			QueueFamilyIndices queueFamilyIndices, string fileLocation, Assembly assembly) =>
				new(debugName, memoryProperties, logicalDevice, transferCommandPool, transferQueue, queueFamilyIndices, fileLocation, "png", 4, VkFormat.FormatR8g8b8a8Srgb, assembly);

		public void Destroy() {
			if (IGraphicsResource.WarnIfDestroyed(this)) { return; }

			Vk.DestroyImageView(logicalDevice, ImageView, null);
			Vk.DestroyImage(logicalDevice, Image, null);
			Vk.FreeMemory(logicalDevice, ImageMemory, null);

			WasDestroyed = true;
		}

		private static void CreateTexture(VkPhysicalDeviceMemoryProperties memoryProperties, VkDevice logicalDevice, VkCommandPool transferCommandPool, VkQueue transferQueue, QueueFamilyIndices queueFamilyIndices,
			StbiImage stbiImage, byte texChannels, VkFormat imageFormat, out VkImage image, out VkDeviceMemory imageMemory) {
			uint width = (uint)stbiImage.Width;
			uint height = (uint)stbiImage.Height;

			VkBufferObject stagingBuffer = new("Temporary Image Staging Buffer", width * height * texChannels, memoryProperties, logicalDevice, VkBufferUsageFlagBits.BufferUsageTransferSrcBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit);

			VkH.MapAndCopyMemory(logicalDevice, stagingBuffer.BufferMemory, stbiImage.Data, 0);

			image = VkH.CreateImage(logicalDevice, imageFormat, VkImageTiling.ImageTilingOptimal, VkImageUsageFlagBits.ImageUsageSampledBit, width, height);
			imageMemory = VkH.CreateDeviceMemory(memoryProperties, logicalDevice, image, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit);
			VkH.BindImageMemory(logicalDevice, image, imageMemory);

			TransferCommandBufferObject transferCommandBuffer = new(logicalDevice, transferCommandPool, transferQueue);
			transferCommandBuffer.BeginCommandBuffer(VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit);

			transferCommandBuffer.TransitionImageLayout(queueFamilyIndices, image, imageFormat, VkImageLayout.ImageLayoutUndefined, VkImageLayout.ImageLayoutTransferDstOptimal);
			transferCommandBuffer.CmdCopyImage(stagingBuffer.Buffer, image, width, height);
			transferCommandBuffer.TransitionImageLayout(queueFamilyIndices, image, imageFormat, VkImageLayout.ImageLayoutTransferDstOptimal, VkImageLayout.ImageLayoutShaderReadOnlyOptimal);

			transferCommandBuffer.EndCommandBuffer();
			transferCommandBuffer.SubmitQueue();
			Vk.QueueWaitIdle(transferQueue);

			transferCommandBuffer.Destroy();
			stagingBuffer.Destroy();
		}
	}
}