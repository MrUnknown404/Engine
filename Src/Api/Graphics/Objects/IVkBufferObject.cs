using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Api.Graphics.Objects {
	[PublicAPI]
	public interface IVkBufferObject : IBufferObject {
		public VkBuffer Buffer { get; }
		public VkDeviceMemory BufferMemory { get; }

		public void Copy<T>(T[] data, ulong offset) where T : unmanaged;
		public void CopyUsingStaging<T>(VkCommandPool transferPool, VkQueue transferQueue, T[] data, ulong offset) where T : unmanaged;
	}
}