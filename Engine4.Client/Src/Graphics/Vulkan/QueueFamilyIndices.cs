namespace Engine4.Client.Graphics.Vulkan;

public readonly record struct QueueFamilyIndices {
	public uint GraphicsFamily { get; }
	public uint TransferFamily { get; }
	public uint PresentFamily { get; }

	internal QueueFamilyIndices(uint graphicsFamily, uint transferFamily, uint presentFamily) {
		GraphicsFamily = graphicsFamily;
		TransferFamily = transferFamily;
		PresentFamily = presentFamily;
	}

	public uint[] ToUniqueFamilies() => new HashSet<uint>([ GraphicsFamily, TransferFamily, PresentFamily, ]).ToArray();
}