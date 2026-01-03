namespace Engine3.Graphics.Vulkan {
	public readonly record struct QueueFamilyIndices {
		public uint? GraphicsFamily { get; }
		public uint? PresentFamily { get; }

		public QueueFamilyIndices(uint? graphicsFamily, uint? presentFamily) {
			GraphicsFamily = graphicsFamily;
			PresentFamily = presentFamily;
		}

		public bool IsValid => GraphicsFamily != null && PresentFamily != null;
	}
}