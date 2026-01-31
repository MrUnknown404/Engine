using Engine3.Client.Graphics.Vulkan.Objects;

namespace Engine3.Client.Graphics.Vulkan {
	public class VulkanSceneRenderer : VulkanRenderer, ISceneRenderer {
		public Scene.Scene? Scene { get; set; }

		public VulkanSceneRenderer(VulkanGraphicsBackend graphicsBackend, VulkanWindow window) : base(graphicsBackend, window) { }

		public override void Setup() { }

		protected override void RecordCommandBuffer(GraphicsCommandBuffer graphicsCommandBuffer, float delta) {
			if (Scene != null) {
				// TODO draw our objects. how the hell is this going to work
			}
		}

		protected override void Cleanup() { }
	}
}