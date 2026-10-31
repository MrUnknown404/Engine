using Engine4.Client.Graphics.Vulkan.Objects;
using Engine4.Client.Utility;
using Engine4.Client.Utility.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan;

public sealed unsafe class LogicalGpu : VulkanResource {
	public VkDevice VkLogicalDevice { get; }
	public Queue GraphicsQueue { get; } // TODO queue objects
	public Queue PresentQueue { get; }
	public Queue TransferQueue { get; }

	public override ulong Handle => (ulong)VkLogicalDevice.Handle;

	private readonly SurfaceReadyPhysicalGpu physicalGpu;

	internal LogicalGpu(SurfaceReadyPhysicalGpu physicalGpu, VulkanSettings vulkanSettings) {
		this.physicalGpu = physicalGpu;
		VkLogicalDevice = CreateLogicalDevice(physicalGpu, vulkanSettings);

		QueueFamilyIndices queueFamilyIndices = physicalGpu.QueueFamilyIndices;
		GraphicsQueue = new(this, queueFamilyIndices.GraphicsFamily);
		PresentQueue = new(this, queueFamilyIndices.PresentFamily);
		TransferQueue = new(this, queueFamilyIndices.TransferFamily);
	}

	[MustUseReturnValue]
	private static VkDevice CreateLogicalDevice(SurfaceReadyPhysicalGpu physicalGpu, VulkanSettings vulkanSettings) {
		uint[] uniqueQueueFamilyIndices = physicalGpu.QueueFamilyIndices.ToUniqueFamilies();
		VkDeviceQueueCreateInfo* deviceQueueCreateInfo = stackalloc VkDeviceQueueCreateInfo[uniqueQueueFamilyIndices.Length];
		float queuePriority = 1f;

		for (int i = 0; i < uniqueQueueFamilyIndices.Length; i++) { deviceQueueCreateInfo[i] = new() { queueFamilyIndex = uniqueQueueFamilyIndices[i], queueCount = 1, pQueuePriorities = &queuePriority, }; }

		VkPhysicalDeviceDescriptorHeapFeaturesEXT physicalDeviceDescriptorHeapFeatures = new() {
				// pNext = ,
				descriptorHeap = VkH.True,
		};

		PhysicalDeviceSettings deviceSettings = vulkanSettings.PhysicalDeviceSettings;

		VkPhysicalDeviceVulkan11Features physicalDeviceVulkan11Features = deviceSettings.Vulkan11Features with {
				pNext = &physicalDeviceDescriptorHeapFeatures, //
				shaderDrawParameters = VkH.True,
		};

		VkPhysicalDeviceVulkan12Features physicalDeviceVulkan12Features = deviceSettings.Vulkan12Features with {
				pNext = &physicalDeviceVulkan11Features, //
				drawIndirectCount = VkH.True,
				scalarBlockLayout = VkH.True,
				shaderSampledImageArrayNonUniformIndexing = VkH.True,
				bufferDeviceAddress = VkH.True,
		};

		VkPhysicalDeviceVulkan13Features physicalDeviceVulkan13Features = deviceSettings.Vulkan13Features with {
				pNext = &physicalDeviceVulkan12Features, //
				synchronization2 = VkH.True,
				dynamicRendering = VkH.True,
		};

		VkPhysicalDeviceVulkan14Features physicalDeviceVulkan14Features = deviceSettings.Vulkan14Features with {
				pNext = &physicalDeviceVulkan13Features, //
				dynamicRenderingLocalRead = VkH.True,
		};

		VkPhysicalDeviceFeatures physicalDeviceFeatures = deviceSettings.PhysicalDeviceFeatures with {
				samplerAnisotropy = VkH.True, //
				multiDrawIndirect = VkH.True,
		};

		using StringArrayUtf8Ptr requiredDeviceExtensionPropertiesPtr = new(vulkanSettings.RequiredDeviceExtensionProperties);

		VkDeviceCreateInfo deviceCreateInfo = new() {
				pNext = &physicalDeviceVulkan14Features,
				queueCreateInfoCount = (uint)uniqueQueueFamilyIndices.Length,
				pQueueCreateInfos = deviceQueueCreateInfo,
				pEnabledFeatures = &physicalDeviceFeatures,
				enabledExtensionCount = (uint)vulkanSettings.RequiredDeviceExtensionProperties.Length,
				ppEnabledExtensionNames = requiredDeviceExtensionPropertiesPtr,
		};

		VkDevice logicalDevice;
		VkH.ThrowIfNotSuccess(Vk.CreateDevice(physicalGpu.VkPhysicalDevice, &deviceCreateInfo, null, &logicalDevice), "Failed to create logical device");
		return logicalDevice;
	}

	[MustUseReturnValue]
	internal VkDeviceMemory CreateDeviceMemory(VkMemoryRequirements2 memoryRequirements2, VkMemoryPropertyFlagBits memoryPropertyFlags) {
		VkMemoryRequirements memoryRequirements = memoryRequirements2.memoryRequirements;

		VkMemoryAllocateFlagsInfo memoryAllocateFlagsInfo = new() {
				/* See https://docs.vulkan.org/refpages/latest/refpages/source/VkMemoryAllocateFlagBits.html
				 * "Setting VK_MEMORY_ALLOCATE_DEVICE_ADDRESS_BIT is not expected to reduce performance on any known implementation.
				 * Applications should feel free to set it on every allocation, to help avoid needing to know in advance whether a device address will be needed for any buffer bound to the memory.
				 * Some development and/or debugging tools, such as a capture/replay layer, might have overhead for memory allocated with this bit, as they might need to track the device addresses of those allocations.
				 * However, this is not expected to be significant enough to be a reason for applications to avoid setting the bit unconditionally."
				 */
				flags = VkMemoryAllocateFlagBits.MemoryAllocateDeviceAddressBit,
		};

		VkMemoryAllocateInfo memoryAllocateInfo = new() {
				pNext = &memoryAllocateFlagsInfo, //
				allocationSize = memoryRequirements.size,
				memoryTypeIndex = FindMemoryType(physicalGpu.MemoryProperties2.memoryProperties, memoryRequirements.memoryTypeBits, memoryPropertyFlags),
		};

		// TODO "It should be noted that in a real world application, you're not supposed to actually call vkAllocateMemory for every individual buffer.
		//  The right way to allocate memory for a large number of objects at the same time is to create a custom allocator that splits up a single allocation
		//  among many different objects by using the offset parameters that we've seen in many functions."
		VkDeviceMemory deviceMemory;
		VkH.ThrowIfNotSuccess(Vk.AllocateMemory(VkLogicalDevice, &memoryAllocateInfo, null, &deviceMemory), "Failed to allocate gpu memory");
		return deviceMemory;

		[MustUseReturnValue]
		static uint FindMemoryType(VkPhysicalDeviceMemoryProperties memoryProperties, uint typeFilter, VkMemoryPropertyFlagBits memoryPropertyFlag) {
			for (int i = 0; i < memoryProperties.memoryTypeCount; i++) {
				if ((typeFilter & (1 << i)) != 0 && (memoryProperties.memoryTypes[i].propertyFlags & memoryPropertyFlag) == memoryPropertyFlag) { return (uint)i; }
			}

			throw new VulkanException("Failed to find suitable memory type");
		}
	}

	public void BindBufferMemory(VkBuffer buffer, VkDeviceMemory deviceMemory) {
		VkBindBufferMemoryInfo bindBufferMemoryInfo = new() { buffer = buffer, memory = deviceMemory, };
		VkH.ThrowIfNotSuccess(Vk.BindBufferMemory2(VkLogicalDevice, 1, &bindBufferMemoryInfo), "Failed to bind buffer memory");
	}

	public void BindImageMemory(VkImage image, VkDeviceMemory deviceMemory) {
		VkBindImageMemoryInfo bindImageMemoryInfo = new() { image = image, memory = deviceMemory, };
		VkH.ThrowIfNotSuccess(Vk.BindImageMemory2(VkLogicalDevice, 1, &bindImageMemoryInfo), "Failed to bind image memory");
	}

	protected internal override void Cleanup() => Vk.DestroyDevice(VkLogicalDevice, null);
}