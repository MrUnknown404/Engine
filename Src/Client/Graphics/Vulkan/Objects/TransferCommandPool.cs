using System.Runtime.InteropServices;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;
using StbiSharp;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public sealed unsafe class TransferCommandPool : CommandPool {
		internal TransferCommandPool(LogicalGpu logicalGpu, VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamilyIndex) : base(logicalGpu, commandPoolCreateFlags, queueFamilyIndex) { }

		[MustUseReturnValue]
		public TransferCommandBuffer CreateCommandBuffer(VkCommandBufferLevel level = VkCommandBufferLevel.CommandBufferLevelPrimary) {
			TransferCommandBuffer commandBuffer = new(LogicalGpu.LogicalDevice, VkCommandPool, level);
			LogicalGpu.AddCommandBuffer(commandBuffer);
			return commandBuffer;
		}

		public void CopyToBuffer<T>(VulkanBuffer dstBuffer, ReadOnlySpan<T> data, ulong offset = 0) where T : unmanaged {
			fixed (T* dataPtr = data) { CopyToBuffer(dstBuffer, dataPtr, (ulong)(sizeof(T) * data.Length), offset); }
		}

		public void CopyToBuffer(VulkanBuffer dstBuffer, byte[] data, ulong offset = 0) {
			fixed (byte* dataPtr = data) { CopyToBuffer(dstBuffer, dataPtr, (ulong)data.Length, offset); }
		}

		public void CopyToBuffer(VulkanBuffer dstBuffer, void* data, ulong size, ulong offset = 0) {
			VulkanBuffer stagingBuffer = LogicalGpu.CreateBuffer("Temporary Staging Buffer", VkBufferUsageFlagBits.BufferUsageTransferSrcBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, size);

			stagingBuffer.Copy(data, size);

			TransferCommandBuffer transferCommandBuffer = CreateCommandBuffer();

			transferCommandBuffer.BeginCommandBuffer(VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit);
			transferCommandBuffer.CmdCopyBuffer(stagingBuffer.Buffer, dstBuffer.Buffer, size, dstOffset: offset);
			transferCommandBuffer.EndCommandBuffer();

			transferCommandBuffer.SubmitQueue(LogicalGpu.TransferQueue);

			LogicalGpu.EnqueueDestroy(transferCommandBuffer);
			LogicalGpu.EnqueueDestroy(stagingBuffer);
		}

		public void CopyToBuffers<T>(CopyBufferInfo[] copyBufferInfo, ReadOnlySpan<T> data) where T : unmanaged {
			fixed (T* dataPtr = data) { CopyToBuffers(copyBufferInfo, dataPtr, (ulong)(sizeof(T) * data.Length)); }
		}

		public void CopyToBuffers(CopyBufferInfo[] copyBufferInfo, byte[] data) {
			fixed (byte* dataPtr = data) { CopyToBuffers(copyBufferInfo, dataPtr, (ulong)data.Length); }
		}

		public void CopyToBuffers(CopyBufferInfo[] copyBufferInfo, void* data, ulong size) {
			VulkanBuffer stagingBuffer = LogicalGpu.CreateBuffer("Temporary Staging Buffer", VkBufferUsageFlagBits.BufferUsageTransferSrcBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, size);

			stagingBuffer.Copy(data, size);

			TransferCommandBuffer transferCommandBuffer = CreateCommandBuffer();

			transferCommandBuffer.BeginCommandBuffer(VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit);

			foreach (CopyBufferInfo copyToBuffersInfo in copyBufferInfo) { transferCommandBuffer.CmdCopyBuffer(stagingBuffer.Buffer, copyToBuffersInfo.Buffer.Buffer, size, dstOffset: copyToBuffersInfo.Offset); }

			transferCommandBuffer.EndCommandBuffer();

			transferCommandBuffer.SubmitQueue(LogicalGpu.TransferQueue);

			LogicalGpu.EnqueueDestroy(transferCommandBuffer);
			LogicalGpu.EnqueueDestroy(stagingBuffer);
		}

		public void CopyToBuffers(CopyDataToBufferInfo[] copyDataToBufferInfo) {
			List<byte> newData = new();
			foreach (CopyDataToBufferInfo copyToInfo in copyDataToBufferInfo) {
				copyToInfo.SrcOffset = (ulong)newData.Count;
				newData.AddRange(copyToInfo.Data);
			}

			ulong bufferSize = (ulong)newData.Count;

			VulkanBuffer stagingBuffer = LogicalGpu.CreateBuffer("Temporary Staging Buffer", VkBufferUsageFlagBits.BufferUsageTransferSrcBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, bufferSize);

			stagingBuffer.Copy(CollectionsMarshal.AsSpan(newData));

			TransferCommandBuffer transferCommandBuffer = CreateCommandBuffer();

			transferCommandBuffer.BeginCommandBuffer(VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit);

			foreach (CopyDataToBufferInfo copyToInfo in copyDataToBufferInfo) {
				transferCommandBuffer.CmdCopyBuffer(stagingBuffer.Buffer, copyToInfo.Buffer.Buffer, (ulong)copyToInfo.Data.Length, copyToInfo.SrcOffset, copyToInfo.Offset); //
			}

			transferCommandBuffer.EndCommandBuffer();

			transferCommandBuffer.SubmitQueue(LogicalGpu.TransferQueue);

			LogicalGpu.EnqueueDestroy(transferCommandBuffer);
			LogicalGpu.EnqueueDestroy(stagingBuffer);
		}

		public void CopyToImage(VulkanImage image, QueueFamilyIndices queueFamilyIndices, VkQueue transferQueue, StbiImage stbiImage) =>
				CopyToImage(image, queueFamilyIndices, transferQueue, (uint)stbiImage.Width, (uint)stbiImage.Height, (byte)stbiImage.NumChannels, stbiImage.Data);

		public void CopyToImage(VulkanImage image, QueueFamilyIndices queueFamilyIndices, VkQueue transferQueue, uint width, uint height, byte channels, ReadOnlySpan<byte> data) {
			fixed (byte* dataPtr = data) { CopyToImage(image, queueFamilyIndices, transferQueue, width, height, channels, dataPtr); }
		}

		public void CopyToImage(VulkanImage image, QueueFamilyIndices queueFamilyIndices, VkQueue transferQueue, uint width, uint height, byte channels, void* data) {
			VulkanBuffer stagingBuffer = LogicalGpu.CreateBuffer("Temporary Image Staging Buffer", VkBufferUsageFlagBits.BufferUsageTransferSrcBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, width * height * channels);

			stagingBuffer.Copy(data, width * height * channels);

			TransferCommandBuffer transferCommandBuffer = CreateCommandBuffer();
			transferCommandBuffer.BeginCommandBuffer(VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit);

			transferCommandBuffer.TransitionImageLayout(queueFamilyIndices, image.Image, image.ImageFormat, VkImageLayout.ImageLayoutUndefined, VkImageLayout.ImageLayoutTransferDstOptimal);
			transferCommandBuffer.CmdCopyImage(stagingBuffer.Buffer, image.Image, width, height);
			transferCommandBuffer.TransitionImageLayout(queueFamilyIndices, image.Image, image.ImageFormat, VkImageLayout.ImageLayoutTransferDstOptimal, VkImageLayout.ImageLayoutShaderReadOnlyOptimal);

			transferCommandBuffer.EndCommandBuffer();
			transferCommandBuffer.SubmitQueue(transferQueue);

			LogicalGpu.EnqueueDestroy(transferCommandBuffer);
			LogicalGpu.EnqueueDestroy(stagingBuffer);
		}

		public class CopyBufferInfo {
			public VulkanBuffer Buffer { get; }
			public ulong Offset { get; init; }

			public CopyBufferInfo(VulkanBuffer buffer) => Buffer = buffer;
		}

		public class CopyDataToBufferInfo {
			public byte[] Data { get; }
			public ulong Size { get; }

			public VulkanBuffer Buffer { get; }
			public ulong Offset { get; init; }

			internal ulong SrcOffset { get; set; }

			public CopyDataToBufferInfo(byte[] data, ulong size, VulkanBuffer buffer) {
				Data = data;
				Size = size;
				Buffer = buffer;
			}

			[MustUseReturnValue]
			public static CopyDataToBufferInfo Copy<T>(VulkanBuffer buffer, T[] data, ulong dstOffset = 0) where T : unmanaged {
				ulong bufferSize = (ulong)(data.Length * sizeof(T));
				byte[] dstData = new byte[bufferSize];

				fixed (T* dataPtr = &data[0]) {
					fixed (byte* dstDataPtr = &dstData[0]) { System.Buffer.MemoryCopy(dataPtr, dstDataPtr, bufferSize, bufferSize); }
				}

				return new(dstData, bufferSize, buffer) { Offset = dstOffset, };
			}
		}
	}
}