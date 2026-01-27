using Engine3.Client.Graphics.Objects;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	[PublicAPI]
	public interface IVkBufferObject : IBufferObject {
		public VkBuffer Buffer { get; }
		public VkDeviceMemory BufferMemory { get; }

		public void CopyUsingStaging<T>(VkCommandPool transferPool, VkQueue transferQueue, ReadOnlySpan<T> data, ulong offset) where T : unmanaged;
	}
}