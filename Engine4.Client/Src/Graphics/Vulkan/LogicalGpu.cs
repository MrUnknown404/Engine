using Engine4.Client.Graphics.Vulkan.Objects;
using Engine4.Client.Utility;
using Engine4.Client.Utility.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan;

public sealed unsafe class LogicalGpu : VulkanResource {
	private readonly VkDevice vkLogicalDevice;

	public VkDevice VkLogicalDevice => vkLogicalDevice;
	public VkQueue GraphicsQueue { get; } // TODO queue objects
	public VkQueue PresentQueue { get; }
	public VkQueue TransferQueue { get; }

	public override ulong Handle => (ulong)VkLogicalDevice.Handle;

	private readonly SurfaceReadyPhysicalGpu physicalGpu;

	internal LogicalGpu(SurfaceReadyPhysicalGpu physicalGpu, VulkanSettings vulkanSettings) {
		this.physicalGpu = physicalGpu;

		QueueFamilyIndices queueFamilyIndices = physicalGpu.QueueFamilyIndices;
		HashSet<uint> queueFamilies = [ queueFamilyIndices.GraphicsFamily, queueFamilyIndices.PresentFamily, queueFamilyIndices.TransferFamily, ];
		VkDeviceQueueCreateInfo* deviceQueueCreateInfo = stackalloc VkDeviceQueueCreateInfo[queueFamilies.Count];
		float queuePriority = 1f;

		int i = 0;
		foreach (uint queueFamily in queueFamilies) {
			deviceQueueCreateInfo[i] = new() { queueFamilyIndex = queueFamily, queueCount = 1, pQueuePriorities = &queuePriority, };
			i++;
		}

		// TODO allow these to be editable
		VkPhysicalDeviceVulkan11Features physicalDeviceVulkan11Features = new() {
				// pNext = ,
				shaderDrawParameters = VkH.True,
		};

		VkPhysicalDeviceVulkan12Features physicalDeviceVulkan12Features = new() {
				pNext = &physicalDeviceVulkan11Features, //
				drawIndirectCount = VkH.True,
				scalarBlockLayout = VkH.True,
				shaderSampledImageArrayNonUniformIndexing = VkH.True,
				bufferDeviceAddress = VkH.True,
		};

		VkPhysicalDeviceVulkan13Features physicalDeviceVulkan13Features = new() {
				pNext = &physicalDeviceVulkan12Features, //
				synchronization2 = VkH.True,
				dynamicRendering = VkH.True,
		};

		VkPhysicalDeviceVulkan14Features physicalDeviceVulkan14Features = new() {
				pNext = &physicalDeviceVulkan13Features, //
				dynamicRenderingLocalRead = VkH.True,
		};

		VkPhysicalDeviceFeatures physicalDeviceFeatures = new() {
				samplerAnisotropy = VkH.True, //
				multiDrawIndirect = VkH.True,
		};

		using StringArrayUtf8Ptr requiredDeviceExtensionPropertiesPtr = new(vulkanSettings.RequiredDeviceExtensionProperties);

		VkDeviceCreateInfo deviceCreateInfo = new() {
				pNext = &physicalDeviceVulkan14Features,
				pQueueCreateInfos = deviceQueueCreateInfo,
				queueCreateInfoCount = (uint)queueFamilies.Count,
				pEnabledFeatures = &physicalDeviceFeatures,
				ppEnabledExtensionNames = requiredDeviceExtensionPropertiesPtr,
				enabledExtensionCount = (uint)vulkanSettings.RequiredDeviceExtensionProperties.Length,
		};

		fixed (VkDevice* pLogicalDevice = &vkLogicalDevice) { VkH.ThrowIfNotSuccess(Vk.CreateDevice(physicalGpu.VkPhysicalDevice, &deviceCreateInfo, null, pLogicalDevice), "Failed to create logical device"); }

		GraphicsQueue = GetDeviceQueue(VkLogicalDevice, queueFamilyIndices.GraphicsFamily);
		PresentQueue = GetDeviceQueue(VkLogicalDevice, queueFamilyIndices.PresentFamily);
		TransferQueue = GetDeviceQueue(VkLogicalDevice, queueFamilyIndices.TransferFamily);

		return;

		[MustUseReturnValue]
		static VkQueue GetDeviceQueue(VkDevice logicalDevice, uint queueFamilyIndex) {
			VkDeviceQueueInfo2 deviceQueueInfo2 = new() { queueFamilyIndex = queueFamilyIndex, };
			VkQueue queue;
			Vk.GetDeviceQueue2(logicalDevice, &deviceQueueInfo2, &queue);
			return queue;
		}
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