using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Engine3.Exceptions;
using Engine3.Utility;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;
using StbiSharp;

namespace Engine3.Graphics.Vulkan.Objects {
	public unsafe class VkImageObject : IGraphicsResource {
		public VkImage Image { get; }
		public VkDeviceMemory ImageMemory { get; }
		public VkImageView ImageView { get; }
		public VkFormat ImageFormat { get; }

		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		private readonly VkDevice logicalDevice;

		public VkImageObject(string debugName, VkPhysicalDevice physicalDevice, VkDevice logicalDevice, VkCommandPool transferCommandPool, VkQueue transferQueue, QueueFamilyIndices queueFamilyIndices, string fileLocation,
			string fileExtension, byte texChannels, VkFormat imageFormat, Assembly assembly) {
			DebugName = debugName;
			ImageFormat = imageFormat;
			this.logicalDevice = logicalDevice;

			using (StbiImage stbiImage = LoadImage(fileLocation, fileExtension, texChannels, assembly)) {
				CreateTexture(physicalDevice, logicalDevice, transferCommandPool, transferQueue, queueFamilyIndices, stbiImage, texChannels, imageFormat, out VkImage image, out VkDeviceMemory imageMemory);
				Image = image;
				ImageMemory = imageMemory;
				ImageView = VkH.CreateImageView(logicalDevice, Image, imageFormat);
			}
		}

		[MustUseReturnValue]
		public static VkImageObject CreateFromRgbaPng(string debugName, VkPhysicalDevice physicalDevice, VkDevice logicalDevice, VkCommandPool transferCommandPool, VkQueue transferQueue, QueueFamilyIndices queueFamilyIndices,
			string fileLocation, Assembly assembly) =>
				new(debugName, physicalDevice, logicalDevice, transferCommandPool, transferQueue, queueFamilyIndices, fileLocation, "png", 4, VkFormat.FormatR8g8b8a8Srgb, assembly);

		public void Destroy() {
			IGraphicsResource.WarnIfDestroyed(this);

			Vk.DestroyImageView(logicalDevice, ImageView, null);
			Vk.DestroyImage(logicalDevice, Image, null);
			Vk.FreeMemory(logicalDevice, ImageMemory, null);

			WasDestroyed = true;
		}

		[MustDisposeResource]
		private static StbiImage LoadImage(string fileLocation, string fileExtension, byte texChannels, Assembly assembly) {
			string fullFileName = $"{fileLocation}.{fileExtension}";
			using Stream? textureStream = AssetH.GetAssetStream($"Textures.{fullFileName}", assembly);
			if (textureStream == null) { throw new Engine3Exception($"Failed to create asset stream at Textures.{fullFileName}"); }

			byte[] data = new byte[textureStream.Length];
			return textureStream.Read(data, 0, data.Length) != data.Length ? throw new Engine3Exception("Texture stream size is not correct") : Stbi.LoadFromMemory(data, texChannels);
		}

		private static void CreateTexture(VkPhysicalDevice physicalDevice, VkDevice logicalDevice, VkCommandPool transferCommandPool, VkQueue transferQueue, QueueFamilyIndices queueFamilyIndices, StbiImage stbiImage,
			byte texChannels, VkFormat imageFormat, out VkImage image, out VkDeviceMemory imageMemory) {
			uint width = (uint)stbiImage.Width;
			uint height = (uint)stbiImage.Height;

			VkBufferObject stagingBuffer = new("Temporary Image Staging Buffer", width * height * texChannels, physicalDevice, logicalDevice, VkBufferUsageFlagBits.BufferUsageTransferSrcBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit);

			VkH.MapAndCopyMemory(logicalDevice, stagingBuffer.BufferMemory, stbiImage.Data, 0);

			VkImageCreateInfo imageCreateInfo = new() {
					imageType = VkImageType.ImageType2d,
					format = imageFormat,
					tiling = VkImageTiling.ImageTilingOptimal,
					initialLayout = VkImageLayout.ImageLayoutUndefined,
					usage = VkImageUsageFlagBits.ImageUsageTransferDstBit | VkImageUsageFlagBits.ImageUsageSampledBit,
					sharingMode = VkSharingMode.SharingModeExclusive,
					samples = VkSampleCountFlagBits.SampleCount1Bit,
					flags = 0,
					extent = new() { width = width, height = height, depth = 1, },
					mipLevels = 1,
					arrayLayers = 1,
			};

			VkImage tempImage;
			VkH.CheckIfSuccess(Vk.CreateImage(logicalDevice, &imageCreateInfo, null, &tempImage), VulkanException.Reason.CreateImage);

			image = tempImage;
			imageMemory = VkH.CreateDeviceMemory(physicalDevice, logicalDevice, image, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit);
			VkH.BindImageMemory(logicalDevice, image, imageMemory);

			TransferCommandBufferObject transferCommandBuffer = new(logicalDevice, transferCommandPool, transferQueue);
			transferCommandBuffer.BeginCommandBuffer(VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit);

			VkImageMemoryBarrier2 imageMemoryBarrier = CreateBeginImageBarrier(queueFamilyIndices.GraphicsFamily, queueFamilyIndices.TransferFamily, image, VkImageLayout.ImageLayoutUndefined,
				VkImageLayout.ImageLayoutTransferDstOptimal);

			transferCommandBuffer.CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier, });

			transferCommandBuffer.CmdCopyImage(stagingBuffer.Buffer, image, width, height);

			imageMemoryBarrier = CreateEndImageBarrier(queueFamilyIndices.GraphicsFamily, queueFamilyIndices.TransferFamily, image, VkImageLayout.ImageLayoutTransferDstOptimal);
			transferCommandBuffer.CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier, });

			transferCommandBuffer.EndCommandBuffer();
			transferCommandBuffer.SubmitQueue();

			transferCommandBuffer.Destroy();
			stagingBuffer.Destroy();

			return;

			[SuppressMessage("ReSharper", "SwitchStatementHandlesSomeKnownEnumValuesWithDefault")]
			[MustUseReturnValue]
			static VkImageMemoryBarrier2 CreateBeginImageBarrier(uint graphicsFamily, uint transferFamily, VkImage image, VkImageLayout oldLayout, VkImageLayout newLayout) {
				VkAccessFlagBits2 srcAccessMask;
				VkAccessFlagBits2 dstAccessMask;
				VkPipelineStageFlagBits2 srcStageMask;
				VkPipelineStageFlagBits2 dstStageMask;

				switch (oldLayout) {
					case VkImageLayout.ImageLayoutUndefined when newLayout == VkImageLayout.ImageLayoutTransferDstOptimal:
						srcAccessMask = 0;
						dstAccessMask = VkAccessFlagBits2.Access2TransferWriteBit;
						srcStageMask = VkPipelineStageFlagBits2.PipelineStage2TopOfPipeBit;
						dstStageMask = VkPipelineStageFlagBits2.PipelineStage2TransferBit;
						break;
					case VkImageLayout.ImageLayoutTransferDstOptimal when newLayout == VkImageLayout.ImageLayoutShaderReadOnlyOptimal:
						srcAccessMask = VkAccessFlagBits2.Access2TransferWriteBit;
						dstAccessMask = VkAccessFlagBits2.Access2ShaderReadBit;
						srcStageMask = VkPipelineStageFlagBits2.PipelineStage2TransferBit;
						dstStageMask = VkPipelineStageFlagBits2.PipelineStage2FragmentShaderBit;
						break;
					default: throw new NotImplementedException();
				}

				return new() {
						oldLayout = oldLayout,
						newLayout = newLayout,
						srcQueueFamilyIndex = transferFamily, //Vk.QueueFamilyIgnored,
						dstQueueFamilyIndex = graphicsFamily,
						image = image,
						subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
						srcAccessMask = srcAccessMask,
						dstAccessMask = dstAccessMask,
						srcStageMask = srcStageMask,
						dstStageMask = dstStageMask,
				};
			}

			[MustUseReturnValue]
			static VkImageMemoryBarrier2 CreateEndImageBarrier(uint graphicsFamily, uint transferFamily, VkImage image, VkImageLayout newLayout) =>
					new() {
							oldLayout = newLayout,
							newLayout = VkImageLayout.ImageLayoutShaderReadOnlyOptimal,
							srcQueueFamilyIndex = transferFamily, //Vk.QueueFamilyIgnored,
							dstQueueFamilyIndex = graphicsFamily,
							image = image,
							subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
							srcAccessMask = VkAccessFlagBits2.Access2TransferWriteBit,
							dstAccessMask = VkAccessFlagBits2.Access2ShaderReadBit,
							srcStageMask = VkPipelineStageFlagBits2.PipelineStage2TransferBit,
							dstStageMask = VkPipelineStageFlagBits2.PipelineStage2FragmentShaderBit,
					};
		}
	}
}