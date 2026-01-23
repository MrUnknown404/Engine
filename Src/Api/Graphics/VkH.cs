using Engine3.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Api.Graphics {
	public static unsafe partial class VkH {
		/// <summary>
		/// Helper method for extracting version information out of a packed uint following Vulkan's Version Specification.<br/><br/>
		/// The variant is packed into bits 31-29.<br/>
		/// The major version is packed into bits 28-22.<br/>
		/// The minor version number is packed into bits 21-12.<br/>
		/// The patch version number is packed into bits 11-0.<br/>
		/// </summary>
		/// <param name="version"> Packed integer containing the other 'out' parameters </param>
		/// <param name="variant"> 3-bit integer </param>
		/// <param name="major"> 7-bit integer </param>
		/// <param name="minor"> 10-bit integer </param>
		/// <param name="patch"> 12-bit integer </param>
		/// <seealso href="https://docs.vulkan.org/spec/latest/chapters/extensions.html#extendingvulkan-coreversions">Relevant Vulkan Specification</seealso>
		[Pure]
		public static void GetApiVersion(uint version, out byte variant, out byte major, out ushort minor, out ushort patch) {
			variant = (byte)(version >> 29);
			major = (byte)((version >> 22) & 0x7FU);
			minor = (ushort)((version >> 12) & 0x3FFU);
			patch = (ushort)(version & 0xFFFU);
		}

		[MustUseReturnValue]
		public static VkSemaphore[] CreateSemaphores(VkDevice logicalDevice, uint count) {
			VkSemaphoreCreateInfo semaphoreCreateInfo = new();
			VkSemaphore[] semaphores = new VkSemaphore[count];

			fixed (VkSemaphore* semaphoresPtr = semaphores) {
				for (int i = 0; i < count; i++) {
					VkResult result = Vk.CreateSemaphore(logicalDevice, &semaphoreCreateInfo, null, &semaphoresPtr[i]);
					if (result != VkResult.Success) { throw new VulkanException($"Failed to create semaphore. {result}"); }
				}
			}

			return semaphores;
		}

		[MustUseReturnValue]
		public static VkSemaphore CreateSemaphore(VkDevice logicalDevice) {
			VkSemaphoreCreateInfo semaphoreCreateInfo = new();
			VkSemaphore semaphore;
			VkResult result = Vk.CreateSemaphore(logicalDevice, &semaphoreCreateInfo, null, &semaphore);
			return result != VkResult.Success ? throw new VulkanException($"Failed to create semaphore. {result}") : semaphore;
		}

		[MustUseReturnValue]
		public static VkFence[] CreateFences(VkDevice logicalDevice, uint count) {
			VkFenceCreateInfo fenceCreateInfo = new() { flags = VkFenceCreateFlagBits.FenceCreateSignaledBit, };
			VkFence[] fences = new VkFence[count];

			fixed (VkFence* fencesPtr = fences) {
				for (int i = 0; i < count; i++) {
					VkResult result = Vk.CreateFence(logicalDevice, &fenceCreateInfo, null, &fencesPtr[i]);
					if (result != VkResult.Success) { throw new VulkanException($"Failed to create fence. {result}"); }
				}
			}

			return fences;
		}

		[MustUseReturnValue]
		public static VkFence CreateFence(VkDevice logicalDevice) {
			VkFenceCreateInfo fenceCreateInfo = new() { flags = VkFenceCreateFlagBits.FenceCreateSignaledBit, };
			VkFence fence;
			VkResult result = Vk.CreateFence(logicalDevice, &fenceCreateInfo, null, &fence);
			return result != VkResult.Success ? throw new VulkanException($"Failed to create fence. {result}") : fence;
		}
	}
}