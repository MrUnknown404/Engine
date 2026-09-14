namespace Engine4.Client.Graphics.Vulkan;

public readonly record struct QueueFamilyIndices {
	public uint GraphicsFamily { get; }
	public uint PresentFamily { get; }
	public uint TransferFamily { get; }

	public QueueFamilyIndices(uint graphicsFamily, uint presentFamily, uint transferFamily) {
		GraphicsFamily = graphicsFamily;
		PresentFamily = presentFamily;
		TransferFamily = transferFamily;
	}
}