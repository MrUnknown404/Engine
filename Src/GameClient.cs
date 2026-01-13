using System.Reflection;
using Engine3.Graphics.Vulkan;
using Engine3.Utils;
using OpenTK.Graphics.Vulkan;

namespace Engine3 {
	public abstract partial class GameClient { // TODO split GL/VK?
		public abstract Version4 Version { get; }
		public Assembly Assembly { get; internal init; } = null!; // Set in Engine3#Start
		public string Name { get; internal init; } = null!; // Set in Engine3#Start

		protected internal abstract void Setup();
		protected internal abstract void Update();
		protected internal abstract void GlRender(float delta);
		protected internal abstract void VkRender(VkCommandBuffer vkGraphicsCommandBuffer, SwapChain swapChain, float delta);

		protected internal abstract void Cleanup();
	}
}