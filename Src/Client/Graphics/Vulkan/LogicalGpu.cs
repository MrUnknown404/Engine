using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Utility.Exceptions;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan;

[PublicAPI]
public sealed unsafe class LogicalGpu : GraphicsResource<LogicalGpu, ulong> { // TODO cleanup all these vulkan classes
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public VkDevice LogicalDevice { get; }
	public VkQueue GraphicsQueue { get; }
	public VkQueue PresentQueue { get; }
	public VkQueue TransferQueue { get; }

	protected override ulong Handle => LogicalDevice.Handle;

	private readonly SurfaceCapablePhysicalGpu physicalGpu;

	internal LogicalGpu(SurfaceCapablePhysicalGpu physicalGpu, VkDevice logicalDevice, VkQueue graphicsQueue, VkQueue presentQueue, VkQueue transferQueue) {
		this.physicalGpu = physicalGpu;
		LogicalDevice = logicalDevice;
		GraphicsQueue = graphicsQueue;
		PresentQueue = presentQueue;
		TransferQueue = transferQueue;

		PrintCreate();
	}

	[MustUseReturnValue]
	public VkDeviceMemory CreateDeviceMemory(VkBuffer buffer, VkMemoryPropertyFlagBits memoryPropertyFlags) {
		VkBufferMemoryRequirementsInfo2 bufferMemoryRequirementsInfo2 = new() { buffer = buffer, };
		VkMemoryRequirements2 memoryRequirements2 = new();
		Vk.GetBufferMemoryRequirements2(LogicalDevice, &bufferMemoryRequirementsInfo2, &memoryRequirements2);
		return CreateDeviceMemory(memoryRequirements2.memoryRequirements, memoryPropertyFlags);
	}

	[MustUseReturnValue]
	public VkDeviceMemory CreateDeviceMemory(VkImage image, VkMemoryPropertyFlagBits memoryPropertyFlags) {
		VkImageMemoryRequirementsInfo2 imageMemoryRequirementsInfo2 = new() { image = image, };
		VkMemoryRequirements2 memoryRequirements2 = new();
		Vk.GetImageMemoryRequirements2(LogicalDevice, &imageMemoryRequirementsInfo2, &memoryRequirements2);
		return CreateDeviceMemory(memoryRequirements2.memoryRequirements, memoryPropertyFlags);
	}

	[MustUseReturnValue]
	public VkDeviceMemory CreateDeviceMemory(VkMemoryRequirements memoryRequirements, VkMemoryPropertyFlagBits memoryPropertyFlags) {
		VkMemoryAllocateInfo memoryAllocateInfo = new() {
				allocationSize = memoryRequirements.size, memoryTypeIndex = FindMemoryType(physicalGpu.PhysicalDeviceMemoryProperties2.memoryProperties, memoryRequirements.memoryTypeBits, memoryPropertyFlags),
		};

		// TODO "It should be noted that in a real world application, you're not supposed to actually call vkAllocateMemory for every individual buffer.
		//  The right way to allocate memory for a large number of objects at the same time is to create a custom allocator that splits up a single allocation
		//  among many different objects by using the offset parameters that we've seen in many functions."
		VkDeviceMemory deviceMemory;
		VkH.CheckIfSuccess(Vk.AllocateMemory(LogicalDevice, &memoryAllocateInfo, null, &deviceMemory), VulkanException.Reason.AllocateMemory);
		return deviceMemory;

		[MustUseReturnValue]
		static uint FindMemoryType(VkPhysicalDeviceMemoryProperties memoryProperties, uint typeFilter, VkMemoryPropertyFlagBits memoryPropertyFlag) {
			for (int i = 0; i < memoryProperties.memoryTypeCount; i++) {
				if ((typeFilter & (1 << i)) != 0 && (memoryProperties.memoryTypes[i].propertyFlags & memoryPropertyFlag) == memoryPropertyFlag) { return (uint)i; }
			}

			throw new Engine3VulkanException("Failed to find suitable memory type");
		}
	}

	public void BindBufferMemory(VkBuffer buffer, VkDeviceMemory deviceMemory) {
		VkBindBufferMemoryInfo bindBufferMemoryInfo = new() { buffer = buffer, memory = deviceMemory, };
		VkH.CheckIfSuccess(Vk.BindBufferMemory2(LogicalDevice, 1, &bindBufferMemoryInfo), VulkanException.Reason.BindBufferMemory);
	}

	public void BindImageMemory(VkImage image, VkDeviceMemory deviceMemory) {
		VkBindImageMemoryInfo bindImageMemoryInfo = new() { image = image, memory = deviceMemory, };
		VkH.CheckIfSuccess(Vk.BindImageMemory2(LogicalDevice, 1, &bindImageMemoryInfo), VulkanException.Reason.BindImageMemory);
	}

	[Obsolete]
	[MustUseReturnValue]
	public DepthImage CreateDepthImage(VulkanResourceProvider resourceProvider, TransferCommandPool transferCommandPool, VkExtent2D extent) => new(physicalGpu, resourceProvider, transferCommandPool, TransferQueue, extent);

	[Obsolete]
	[MustUseReturnValue]
	public VkSemaphore[] CreateSemaphores(uint count) { // TODO auto resource
		VkSemaphoreCreateInfo semaphoreCreateInfo = new();
		VkSemaphore[] semaphores = new VkSemaphore[count];

		fixed (VkSemaphore* semaphoresPtr = semaphores) {
			for (uint i = 0; i < count; i++) { VkH.CheckIfSuccess(Vk.CreateSemaphore(LogicalDevice, &semaphoreCreateInfo, null, &semaphoresPtr[i]), VulkanException.Reason.CreateSemaphore); }
		}

		return semaphores;
	}

	[Obsolete]
	[MustUseReturnValue]
	public VkSemaphore CreateSemaphore() { // TODO auto resource
		VkSemaphoreCreateInfo semaphoreCreateInfo = new();
		VkSemaphore semaphore;
		VkH.CheckIfSuccess(Vk.CreateSemaphore(LogicalDevice, &semaphoreCreateInfo, null, &semaphore), VulkanException.Reason.CreateSemaphore);
		return semaphore;
	}

	[Obsolete]
	[MustUseReturnValue]
	public VkFence[] CreateFences(uint count) { // TODO auto resource
		VkFenceCreateInfo fenceCreateInfo = new() { flags = VkFenceCreateFlagBits.FenceCreateSignaledBit, };
		VkFence[] fences = new VkFence[count];

		fixed (VkFence* fencesPtr = fences) {
			for (uint i = 0; i < count; i++) { VkH.CheckIfSuccess(Vk.CreateFence(LogicalDevice, &fenceCreateInfo, null, &fencesPtr[i]), VulkanException.Reason.CreateFence); }
		}

		return fences;
	}

	[Obsolete]
	[MustUseReturnValue]
	public VkFence CreateFence() { // TODO auto resource
		VkFenceCreateInfo fenceCreateInfo = new() { flags = VkFenceCreateFlagBits.FenceCreateSignaledBit, };
		VkFence fence;
		VkH.CheckIfSuccess(Vk.CreateFence(LogicalDevice, &fenceCreateInfo, null, &fence), VulkanException.Reason.CreateFence);
		return fence;
	}

	protected override void Cleanup() => Vk.DestroyDevice(LogicalDevice, null);
}