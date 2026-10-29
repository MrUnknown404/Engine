using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public unsafe class VulkanBuffer : VulkanResource {
	private VkBuffer vkBuffer;
	private VkDeviceMemory deviceMemory;

	public VkBuffer VkBuffer => vkBuffer;
	public VkDeviceMemory DeviceMemory => deviceMemory;
	public ulong Size { get; private set; }

	public override ulong Handle => VkBuffer.Handle;

	private readonly LogicalGpu logicalGpu;
	private readonly VkBufferCreateFlagBits createFlags;
	private readonly VkBufferUsageFlagBits2 usageFlags;
	private readonly VkMemoryPropertyFlagBits memoryPropertyFlags;

	internal VulkanBuffer(string debugName, LogicalGpu logicalGpu, ulong size, VkBufferUsageFlagBits2 usageFlags, VkBufferCreateFlagBits createFlags, VkMemoryPropertyFlagBits memoryPropertyFlags) : base(debugName) {
		this.logicalGpu = logicalGpu;
		this.createFlags = createFlags;
		CreateBuffer(logicalGpu, size, usageFlags, createFlags, memoryPropertyFlags, out vkBuffer, out deviceMemory);
		Size = size;
		this.usageFlags = usageFlags;
		this.memoryPropertyFlags = memoryPropertyFlags;
	}

	public void Copy(void* data, ulong size, ulong offset) {
		void* dstPtr = MapMemory(size, offset);
		Buffer.MemoryCopy(data, dstPtr, size, size);
		UnmapMemory();
	}

	public void Copy(ReadOnlySpan<byte> data, ulong srcOffset = 0, ulong dstOffset = 0) {
		fixed (byte* pData = data) { Copy(pData + srcOffset, (ulong)data.Length, dstOffset); }
	}

	public void Copy<T>(ReadOnlySpan<T> data, ulong srcOffset = 0, ulong dstOffset = 0) where T : unmanaged {
		fixed (T* pData = data) { Copy(pData + srcOffset, (ulong)(data.Length * sizeof(T)), dstOffset); }
	}

	public void Resize(ulong size) {
		Vk.DeviceWaitIdle(logicalGpu.VkLogicalDevice); // clean assumes this was already called
		Cleanup();

		CreateBuffer(logicalGpu, size, usageFlags, createFlags, memoryPropertyFlags, out vkBuffer, out deviceMemory);
		Size = size;
	}

	[MustUseReturnValue]
	public void* MapMemory(ulong size, ulong offset = 0) {
		VkMemoryMapInfo memoryMapInfo = new() { memory = DeviceMemory, size = size, offset = offset, };
		void* dstPtr;
		Vk.MapMemory2(logicalGpu.VkLogicalDevice, &memoryMapInfo, &dstPtr);
		return dstPtr;
	}

	public void UnmapMemory() {
		VkMemoryUnmapInfo memoryUnmapInfo = new() { memory = DeviceMemory, };
		Vk.UnmapMemory2(logicalGpu.VkLogicalDevice, &memoryUnmapInfo);
	}

	protected internal override void Cleanup() {
		VkDevice logicalDevice = logicalGpu.VkLogicalDevice;
		Vk.DestroyBuffer(logicalDevice, VkBuffer, null);
		Vk.FreeMemory(logicalDevice, DeviceMemory, null);
	}

	private static void CreateBuffer(LogicalGpu logicalGpu, ulong size, VkBufferUsageFlagBits2 usageFlags, VkBufferCreateFlagBits bufferCreateFlags, VkMemoryPropertyFlagBits memoryPropertyFlags, out VkBuffer vkBuffer,
		out VkDeviceMemory deviceMemory) {
		VkBufferUsageFlags2CreateInfo usageFlags2Create = new() { usage = usageFlags, };

		VkBufferCreateInfo bufferCreateInfo = new() {
				pNext = &usageFlags2Create, //
				size = size,
				flags = bufferCreateFlags,
				sharingMode = VkSharingMode.SharingModeExclusive,
				// pQueueFamilyIndices = , TODO support concurrent
				// queueFamilyIndexCount = ,
		};

		VkDevice logicalDevice = logicalGpu.VkLogicalDevice;
		fixed (VkBuffer* pVkBuffer = &vkBuffer) { VkH.ThrowIfNotSuccess(Vk.CreateBuffer(logicalDevice, &bufferCreateInfo, null, pVkBuffer), "Failed to create buffer"); }

		VkBufferMemoryRequirementsInfo2 bufferMemoryRequirementsInfo2 = new() { buffer = vkBuffer, };
		VkMemoryRequirements2 memoryRequirements2 = new();
		Vk.GetBufferMemoryRequirements2(logicalDevice, &bufferMemoryRequirementsInfo2, &memoryRequirements2);

		// TODO "It should be noted that in a real world application, you're not supposed to actually call vkAllocateMemory for every individual buffer.
		//  The right way to allocate memory for a large number of objects at the same time is to create a custom allocator that splits up a single allocation
		//  among many different objects by using the offset parameters that we've seen in many functions."
		deviceMemory = logicalGpu.CreateDeviceMemory(memoryRequirements2, memoryPropertyFlags);
		logicalGpu.BindBufferMemory(vkBuffer, deviceMemory);
	}
}