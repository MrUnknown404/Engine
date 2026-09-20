using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics._Test;

public class RenderTarget {
	private readonly VkImage colorImage;
	private readonly VkDeviceMemory colorMemory;
	private readonly VkImageView colorView;

	private readonly VkImage? depthImage;
	private readonly VkDeviceMemory? depthMemory;
	private readonly VkImageView? depthView;

	public VkImageView ColorView => colorView;
	public VkImageView? DepthView => depthView;

	public ushort Width { get; }
	public ushort Height { get; }

	public RenderTarget(ushort width, ushort height, VkImage colorImage, VkDeviceMemory colorMemory, VkImageView colorView, VkImage? depthImage, VkDeviceMemory? depthMemory, VkImageView? depthView) {
		Width = width;
		Height = height;
		this.colorImage = colorImage;
		this.colorMemory = colorMemory;
		this.colorView = colorView;
		this.depthImage = depthImage;
		this.depthMemory = depthMemory;
		this.depthView = depthView;
	}

	public RenderTarget(ushort width, ushort height, bool createDepth) {
		Width = width;
		Height = height;

		CreateColorResources(out colorImage, out colorMemory, out colorView);

		if (createDepth) {
			CreateDepthResources(out VkImage depthImage, out VkDeviceMemory depthMemory, out VkImageView depthView);
			this.depthImage = depthImage;
			this.depthMemory = depthMemory;
			this.depthView = depthView;
		}
	}

	private static void CreateColorResources(out VkImage image, out VkDeviceMemory deviceMemory, out VkImageView imageView) {
		// TODO
		throw new NotImplementedException();
	}

	private static void CreateDepthResources(out VkImage image, out VkDeviceMemory deviceMemory, out VkImageView imageView) {
		// TODO
		throw new NotImplementedException();
	}
}