using System.Diagnostics.CodeAnalysis;

namespace Engine3.Graphics.Vulkan {
	public readonly record struct QueueFamilyIndices {
		public required uint GraphicsFamily { get; init; }
		public required uint PresentFamily { get; init; }

		[SetsRequiredMembers]
		public QueueFamilyIndices(uint graphicsFamily, uint presentFamily) {
			GraphicsFamily = graphicsFamily;
			PresentFamily = presentFamily;
		}
	}
}