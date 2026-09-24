using Engine4.Client.Utility;
using Engine4.Client.Utility.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan;

public unsafe class LogicalGpu {
	internal VkDevice VkLogicalDevice { get; }
	internal VkQueue GraphicsQueue { get; } // TODO queue objects?
	internal VkQueue PresentQueue { get; }
	internal VkQueue TransferQueue { get; }

	internal VulkanResourceManager ResourceManager { get; }
	private readonly SurfaceReadyPhysicalGpu physicalGpu;

	internal LogicalGpu(SurfaceReadyPhysicalGpu physicalGpu, VulkanManager vulkanManager) {
		this.physicalGpu = physicalGpu;
		ResourceManager = new(physicalGpu, this);

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
		VkPhysicalDeviceVulkan11Features physicalDeviceVulkan11Features = new() { shaderDrawParameters = VkH.True, };
		VkPhysicalDeviceVulkan12Features physicalDeviceVulkan12Features = new() { pNext = &physicalDeviceVulkan11Features, drawIndirectCount = VkH.True, scalarBlockLayout = VkH.True, };
		VkPhysicalDeviceVulkan13Features physicalDeviceVulkan13Features = new() { pNext = &physicalDeviceVulkan12Features, synchronization2 = VkH.True, dynamicRendering = VkH.True, };
		VkPhysicalDeviceVulkan14Features physicalDeviceVulkan14Features = new() { pNext = &physicalDeviceVulkan13Features, dynamicRenderingLocalRead = VkH.True, };
		VkPhysicalDeviceFeatures physicalDeviceFeatures = new() { samplerAnisotropy = VkH.True, multiDrawIndirect = VkH.True, };

		using StringArrayUtf8Ptr requiredDeviceExtensionPropertiesPtr = new(vulkanManager.RequiredDeviceExtensionProperties);

		VkDeviceCreateInfo deviceCreateInfo = new() {
				pNext = &physicalDeviceVulkan14Features,
				pQueueCreateInfos = deviceQueueCreateInfo,
				queueCreateInfoCount = (uint)queueFamilies.Count,
				pEnabledFeatures = &physicalDeviceFeatures,
				ppEnabledExtensionNames = requiredDeviceExtensionPropertiesPtr,
				enabledExtensionCount = (uint)vulkanManager.RequiredDeviceExtensionProperties.Length,
		};

		VkDevice logicalDevice;
		VkH.CheckSuccess(Vk.CreateDevice(physicalGpu.VkPhysicalDevice, &deviceCreateInfo, null, &logicalDevice), "Failed to create logical device");

		VkLogicalDevice = logicalDevice;
		GraphicsQueue = GetDeviceQueue(logicalDevice, queueFamilyIndices.GraphicsFamily);
		PresentQueue = GetDeviceQueue(logicalDevice, queueFamilyIndices.PresentFamily);
		TransferQueue = GetDeviceQueue(logicalDevice, queueFamilyIndices.TransferFamily);

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

		VkMemoryAllocateInfo memoryAllocateInfo = new() {
				allocationSize = memoryRequirements.size, memoryTypeIndex = FindMemoryType(physicalGpu.PhysicalDeviceMemoryProperties2.memoryProperties, memoryRequirements.memoryTypeBits, memoryPropertyFlags),
		};

		// TODO "It should be noted that in a real world application, you're not supposed to actually call vkAllocateMemory for every individual buffer.
		//  The right way to allocate memory for a large number of objects at the same time is to create a custom allocator that splits up a single allocation
		//  among many different objects by using the offset parameters that we've seen in many functions."
		VkDeviceMemory deviceMemory;
		VkH.CheckSuccess(Vk.AllocateMemory(VkLogicalDevice, &memoryAllocateInfo, null, &deviceMemory), "Failed to allocate gpu memory");
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
		VkH.CheckSuccess(Vk.BindBufferMemory2(VkLogicalDevice, 1, &bindBufferMemoryInfo), "Failed to bind buffer memory");
	}

	public void BindImageMemory(VkImage image, VkDeviceMemory deviceMemory) {
		VkBindImageMemoryInfo bindImageMemoryInfo = new() { image = image, memory = deviceMemory, };
		VkH.CheckSuccess(Vk.BindImageMemory2(VkLogicalDevice, 1, &bindImageMemoryInfo), "Failed to bind image memory");
	}

	internal void Cleanup() {
		ResourceManager.Cleanup();

		Vk.DestroyDevice(VkLogicalDevice, null);
	}
}