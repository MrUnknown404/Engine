using OpenTK.Graphics.Vulkan;

namespace Engine3.Api.Graphics.Objects {
	public interface IVkBufferObject {
		public VkBuffer Buffer { get; }
		public VkDeviceMemory BufferMemory { get; }

		public void CopyUsingStaging<T>(VkCommandPool transferPool, VkQueue transferQueue, T[] data, ulong offset = 0) where T : unmanaged;
	}
}