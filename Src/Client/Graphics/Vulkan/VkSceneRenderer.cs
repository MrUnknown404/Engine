using Engine3.Client.Graphics.Vulkan.Objects;

namespace Engine3.Client.Graphics.Vulkan {
	public class VkSceneRenderer : VkRenderer, ISceneRenderer {
		public Scene.Scene? Scene { get; set; }

		public VkSceneRenderer(VulkanGraphicsBackend graphicsBackend, VkWindow window) : base(graphicsBackend, window) { }

		public override void Setup() { }

		protected override void RecordCommandBuffer(GraphicsCommandBufferObject graphicsCommandBuffer, float delta) {
			if (Scene != null) {
				// TODO draw our objects. how the hell is this going to work
			}
		}

		protected override void Cleanup() { }
	}
}