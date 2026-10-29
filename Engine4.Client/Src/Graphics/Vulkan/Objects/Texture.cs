using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public unsafe class Texture : VulkanResource { // TODO
	public override ulong Handle => Image.Handle;

	public VkImage Image { get; }
	public VkImageViewCreateInfo ViewCreateInfo { get; }
	public VkFormat Format { get; }
	public VkImageTiling Tiling { get; }
	public VkImageAspectFlagBits AspectMask { get; }

	public ushort Width { get; }
	public ushort Height { get; }

	private readonly LogicalGpu logicalGpu;

	internal Texture(string debugName, LogicalGpu logicalGpu, ushort width, ushort height, VkFormat imageFormat, VkImageTiling imageTiling, VkImageUsageFlagBits usageFlags,
		VkImageAspectFlagBits aspectMask) : base(debugName) {
		this.logicalGpu = logicalGpu;
		Image = CreateImage(logicalGpu, width, height, imageFormat, imageTiling, usageFlags, aspectMask);
		ViewCreateInfo = CreateViewCreateInfo(Image, Format, AspectMask);
		Width = width;
		Height = height;
		Format = imageFormat;
		Tiling = imageTiling;
		AspectMask = aspectMask;
	}

	protected internal override void Cleanup() {
		VkDevice logicalDevice = logicalGpu.VkLogicalDevice;

		// Vk.DestroyImageView(logicalDevice, ImageView, null);
		Vk.DestroyImage(logicalDevice, Image, null);
		// Vk.FreeMemory(logicalDevice, ImageMemory, null);
	}

	[MustUseReturnValue]
	private static VkImage CreateImage(LogicalGpu logicalGpu, ushort width, ushort height, VkFormat imageFormat, VkImageTiling imageTiling, VkImageUsageFlagBits usageFlags, VkImageAspectFlagBits aspectMask) {
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

		VkImage image;
		VkH.ThrowIfNotSuccess(Vk.CreateImage(logicalGpu.VkLogicalDevice, &imageCreateInfo, null, &image), ""); // todo explain
		return image; // TODO device memory
	}

	[MustUseReturnValue]
	private static VkImageViewCreateInfo CreateViewCreateInfo(VkImage image, VkFormat format, VkImageAspectFlagBits aspectMask) =>
			new() {
					image = image,
					viewType = VkImageViewType.ImageViewType2D,
					format = format,
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
}