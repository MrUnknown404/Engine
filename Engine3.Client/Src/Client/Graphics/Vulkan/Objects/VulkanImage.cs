using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Client.Graphics.Vulkan.Objects;

public sealed unsafe class VulkanImage : NamedGraphicsResource<VulkanImage, ulong> {
	public VkImage Image { get; }
	public VkDeviceMemory ImageMemory { get; }
	public VkImageView ImageView { get; }
	public VkFormat ImageFormat { get; }

	protected override ulong Handle => Image.Handle;

	private readonly LogicalGpu logicalGpu;

	internal VulkanImage(string debugName, LogicalGpu logicalGpu, VkImage image, VkDeviceMemory imageMemory, VkImageView imageView, VkFormat imageFormat) : base(debugName) {
		this.logicalGpu = logicalGpu;
		Image = image;
		ImageMemory = imageMemory;
		ImageView = imageView;
		ImageFormat = imageFormat;

		PrintCreate();
	}

	protected override void Cleanup() {
		VkDevice logicalDevice = logicalGpu.LogicalDevice;

		Vk.DestroyImageView(logicalDevice, ImageView, null);
		Vk.DestroyImage(logicalDevice, Image, null);
		Vk.FreeMemory(logicalDevice, ImageMemory, null);
	}
}