using Engine4.Client.Utility;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public unsafe class LogicalGpu {
	public VkDevice VkLogicalDevice { get; } // TODO private
	public VkQueue GraphicsQueue { get; } // TODO private
	public VkQueue PresentQueue { get; } // TODO private
	public VkQueue TransferQueue { get; } // TODO private

	private readonly BoundPhysicalGpu physicalGpu;

	internal LogicalGpu(BoundPhysicalGpu physicalGpu, VulkanManager vulkanManager) {
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
		VkPhysicalDeviceVulkan11Features physicalDeviceVulkan11Features = new() { shaderDrawParameters = VkH.True, };
		VkPhysicalDeviceVulkan12Features physicalDeviceVulkan12Features = new() { pNext = &physicalDeviceVulkan11Features, drawIndirectCount = VkH.True, scalarBlockLayout = VkH.True, };
		VkPhysicalDeviceVulkan13Features physicalDeviceVulkan13Features = new() { pNext = &physicalDeviceVulkan12Features, synchronization2 = VkH.True, dynamicRendering = VkH.True, };
		VkPhysicalDeviceVulkan14Features physicalDeviceVulkan14Features = new() { pNext = &physicalDeviceVulkan13Features, };
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
		if (Vk.CreateDevice(physicalGpu.VkPhysicalDevice, &deviceCreateInfo, null, &logicalDevice) != VkResult.Success) { throw new Exception(); } // TODO exception

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

	internal void Cleanup() => Vk.DestroyDevice(VkLogicalDevice, null);
}