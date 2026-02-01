using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.GameObject;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan {
	public class VulkanSceneRenderer : VulkanRenderer, ISceneRenderer {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public Scene.Scene? Scene { get; set; }

		private bool isGameObjectCacheDirty;

		private readonly Dictionary<VkPipeline, IEnumerable<IGameObject>> cachedSortedGameObjects = new();

		private readonly Camera camera;

		public VulkanSceneRenderer(VulkanGraphicsBackend graphicsBackend, VulkanWindow window) : base(graphicsBackend, window) {
			// TODO temp
			camera = new PerspectiveCamera((float)SwapChain.Extent.width / SwapChain.Extent.height, 0.01f, 10f) { Position = new(0, 0, 2.5f), YawDegrees = 270, };
		}

		public override void Setup() { }

		protected override bool AcquireNextImage(FrameData frameData, out uint swapChainImageIndex) {
			if (Scene == null) {
				swapChainImageIndex = 0;
				return false;
			}

			if (isGameObjectCacheDirty) {
				cachedSortedGameObjects.Clear();
				SortGameObjects();
			}

			if (cachedSortedGameObjects.Count == 0) {
				swapChainImageIndex = 0;
				return false;
			}

			return base.AcquireNextImage(frameData, out swapChainImageIndex);
		}

		protected override void RecordCommandBuffer(GraphicsCommandBuffer graphicsCommandBuffer, float delta) {
			graphicsCommandBuffer.CmdSetViewport(0, 0, SwapChain.Extent.width, SwapChain.Extent.height, 0, 1);
			graphicsCommandBuffer.CmdSetScissor(SwapChain.Extent, new(0, 0));

			foreach ((VkPipeline pipeline, IEnumerable<IGameObject> gameObjects) in cachedSortedGameObjects) {
				graphicsCommandBuffer.CmdBindGraphicsPipeline(pipeline);

				foreach (IGameObject gameObject in gameObjects) {
					if (gameObject.Model is { } model) {
						Mesh[] meshes = model.Meshes;
						if (meshes.Length == 0) { continue; }

						// draw model
					}
				}
			}
		}

		private void SortGameObjects() {
			//
		}

		protected override void Cleanup() { }
	}
}