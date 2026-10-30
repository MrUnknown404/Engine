using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public sealed unsafe class Texture : NamedVulkanResource {
	private readonly VkImage image;
	private readonly VkImageView imageView;
	private readonly VkDeviceMemory deviceMemory;

	public VkImage Image => image;
	public VkImageView ImageView => imageView;
	public VkDeviceMemory DeviceMemory => deviceMemory;

	public VkFormat ImageFormat { get; }
	public VkImageTiling ImageTiling { get; }
	public VkImageAspectFlagBits ImageAspectMask { get; }

	public ushort Width { get; }
	public ushort Height { get; }

	public override ulong Handle => Image.Handle;

	private readonly LogicalGpu logicalGpu;

	internal Texture(string debugName, LogicalGpu logicalGpu, ushort width, ushort height, VkFormat imageFormat, VkImageTiling imageTiling, VkImageUsageFlagBits usageFlags,
		VkImageAspectFlagBits imageAspectMask) : base(debugName) {
		this.logicalGpu = logicalGpu;
		Width = width;
		Height = height;
		ImageFormat = imageFormat;
		ImageTiling = imageTiling;
		ImageAspectMask = imageAspectMask;

		CreateImage(logicalGpu, width, height, imageFormat, imageTiling, usageFlags, imageAspectMask, out image, out imageView, out deviceMemory);
	}

	protected internal override void Cleanup() {
		VkDevice logicalDevice = logicalGpu.VkLogicalDevice;

		Vk.DestroyImageView(logicalDevice, ImageView, null);
		Vk.DestroyImage(logicalDevice, Image, null);
		Vk.FreeMemory(logicalDevice, DeviceMemory, null);
	}

	private static void CreateImage(LogicalGpu logicalGpu, ushort width, ushort height, VkFormat imageFormat, VkImageTiling imageTiling, VkImageUsageFlagBits usageFlags, VkImageAspectFlagBits aspectMask,
		out VkImage image, out VkImageView imageView, out VkDeviceMemory deviceMemory) {
		VkDevice logicalDevice = logicalGpu.VkLogicalDevice;

		// image
		VkImageCreateInfo imageCreateInfo = new() {
				imageType = VkImageType.ImageType2D,
				format = imageFormat,
				tiling = imageTiling,
				initialLayout = VkImageLayout.ImageLayoutUndefined,
				usage = usageFlags | VkImageUsageFlagBits.ImageUsageTransferDstBit,
				sharingMode = VkSharingMode.SharingModeExclusive,
				samples = VkSampleCountFlagBits.SampleCount1Bit,
				flags = 0,
				extent = new() { width = width, height = height, depth = 1, },
				mipLevels = 1,
				arrayLayers = 1,
		};

		fixed (VkImage* pImage = &image) { VkH.ThrowIfNotSuccess(Vk.CreateImage(logicalDevice, &imageCreateInfo, null, pImage), "Failed to create image"); }

		// memory
		VkImageMemoryRequirementsInfo2 imageMemoryRequirements = new() { image = image, };
		VkMemoryRequirements2 memoryRequirements2 = new();
		Vk.GetImageMemoryRequirements2(logicalGpu.VkLogicalDevice, &imageMemoryRequirements, &memoryRequirements2);

		deviceMemory = logicalGpu.CreateDeviceMemory(memoryRequirements2, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit);
		logicalGpu.BindImageMemory(image, deviceMemory);

		// view
		VkImageViewCreateInfo imageViewCreateInfo = new() {
				image = image,
				viewType = VkImageViewType.ImageViewType2D,
				format = imageFormat,
				components = new() {
						r = VkComponentSwizzle.ComponentSwizzleIdentity, //
						g = VkComponentSwizzle.ComponentSwizzleIdentity,
						b = VkComponentSwizzle.ComponentSwizzleIdentity,
						a = VkComponentSwizzle.ComponentSwizzleIdentity,
				},
				subresourceRange = new() {
						aspectMask = aspectMask, //
						baseMipLevel = 0,
						levelCount = 1,
						baseArrayLayer = 0,
						layerCount = 1,
				},
		};

		fixed (VkImageView* pImageView = &imageView) { VkH.ThrowIfNotSuccess(Vk.CreateImageView(logicalDevice, &imageViewCreateInfo, null, pImageView), "Failed to create image view"); }
	}
}