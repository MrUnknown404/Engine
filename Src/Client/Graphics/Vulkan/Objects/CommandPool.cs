using Engine3.Exceptions;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public unsafe class CommandPool : GraphicsResource<CommandPool, ulong> {
		public VkCommandPool VkCommandPool { get; }

		protected LogicalGpu LogicalGpu { get; }
		protected VulkanResourceProvider GraphicsResourceProvider { get; }

		protected override ulong Handle => VkCommandPool.Handle;

		protected CommandPool(LogicalGpu logicalGpu, VulkanResourceProvider graphicsResourceProvider, VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamilyIndex) {
			LogicalGpu = logicalGpu;
			GraphicsResourceProvider = graphicsResourceProvider;

			VkCommandPoolCreateInfo commandPoolCreateInfo = new() { flags = commandPoolCreateFlags, queueFamilyIndex = queueFamilyIndex, };
			VkCommandPool commandPool;
			VkH.CheckIfSuccess(Vk.CreateCommandPool(logicalGpu.LogicalDevice, &commandPoolCreateInfo, null, &commandPool), VulkanException.Reason.CreateCommandPool);
			VkCommandPool = commandPool;

			PrintCreate();
		}

		protected sealed override void PrintCreate() => base.PrintCreate();

		protected override void Cleanup() => Vk.DestroyCommandPool(LogicalGpu.LogicalDevice, VkCommandPool, null);
	}
}