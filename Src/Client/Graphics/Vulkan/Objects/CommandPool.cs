using Engine3.Exceptions;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public sealed unsafe class CommandPool : GraphicsResource<CommandPool, ulong> {
		public VkCommandPool VkCommandPool { get; }

		private readonly VkDevice logicalDevice;

		protected override ulong Handle => VkCommandPool.Handle;

		public CommandPool(VkDevice logicalDevice, VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamilyIndex) {
			this.logicalDevice = logicalDevice;

			VkCommandPoolCreateInfo commandPoolCreateInfo = new() { flags = commandPoolCreateFlags, queueFamilyIndex = queueFamilyIndex, };
			VkCommandPool commandPool;
			VkH.CheckIfSuccess(Vk.CreateCommandPool(logicalDevice, &commandPoolCreateInfo, null, &commandPool), VulkanException.Reason.CreateCommandPool);
			VkCommandPool = commandPool;

			PrintCreate();
		}

		protected override void Cleanup() => Vk.DestroyCommandPool(logicalDevice, VkCommandPool, null);
	}
}