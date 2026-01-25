using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Exceptions {
	[PublicAPI]
	public class VulkanException : Exception, IEnumException<VulkanException.Reason> {
		public Reason ReasonEnum { get; }

		public VulkanException(Reason reason) : base(ReasonToString(reason)) => ReasonEnum = reason;
		public VulkanException(Reason reason, VkResult result, params object?[] args) : base(string.Format(ReasonToString(reason), result, args)) => ReasonEnum = reason;

		public static string ReasonToString(Reason reason) {
			const string DefaultMessage = "failed with the follow result: {0}";

			return reason switch {
					Reason.Unknown => "Unknown error type",
					Reason.CreateShaderModule => $"vkCreateShaderModule() {DefaultMessage}",
					Reason.QueueSubmit => $"vkQueueSubmit() {DefaultMessage}",
					Reason.AllocateCommandBuffer => $"vkAllocateCommandBuffers() {DefaultMessage}",
					Reason.CreateInstance => $"vkCreateInstance() {DefaultMessage}",
					Reason.CreateDebugMessenger => $"vkCreateDebugUtilsMessengerEXT() {DefaultMessage}",
					Reason.AllocateDescriptorSets => $"vkAllocateDescriptorSets() {DefaultMessage}",
					Reason.CreateDescriptorSetLayout => $"vkCreateDescriptorSetLayout() {DefaultMessage}",
					Reason.CreateDescriptorPool => $"vkCreateDescriptorPool() {DefaultMessage}",
					Reason.CreatePipelineLayout => $"vkCreatePipelineLayout() {DefaultMessage}",
					Reason.CreateGraphicsPipeline => $"vkCreateGraphicsPipelines() {DefaultMessage}",
					Reason.CreateSwapChain => $"vkCreateSwapchainKHR() {DefaultMessage}",
					Reason.GetSwapChainImages => $"vkGetSwapchainImagesKHR() {DefaultMessage}",
					Reason.CreateImageView => $"vkCreateImageView() {DefaultMessage}",
					Reason.CreateImageViews => "vkCreateImageView() failed to create image view {1} with the follow result: {0}",
					Reason.CreateSemaphore => $"vkCreateSemaphore() {DefaultMessage}",
					Reason.CreateFence => $"vkCreateFence() {DefaultMessage}",
					Reason.CreateBuffer => $"vkCreateBuffer() {DefaultMessage}",
					Reason.BindBufferMemory => $"vkBindBufferMemory2() {DefaultMessage}",
					Reason.AllocateMemory => $"vkAllocateMemory() {DefaultMessage}",
					Reason.AcquireNextImage => $"vkAcquireNextImageKHR() {DefaultMessage}",
					Reason.BeginCommandBuffer => $"vkBeginCommandBuffer() {DefaultMessage}",
					Reason.EndCommandBuffer => $"vkEndCommandBuffer() {DefaultMessage}",
					Reason.QueuePresent => $"vkQueuePresentKHR() {DefaultMessage}",
					Reason.CreateCommandPool => $"vkCreateCommandPool() {DefaultMessage}",
					Reason.AllocateCommandBuffers => $"vkAllocateCommandBuffers() {DefaultMessage}",
					Reason.CreateSurface => $"Toolkit.Vulkan.CreateWindowSurface() {DefaultMessage}",
					Reason.CreateLogicalDevice => $"vkCreateDevice {DefaultMessage}",
					Reason.CreateImage => $"vkCreateImage {DefaultMessage}",
					Reason.BindImageMemory => $"vkBindImageMemory2 {DefaultMessage}",
					Reason.CreateTextureSampler => $"vkCreateSampler {DefaultMessage}",
					_ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
			};
		}

		public enum Reason : uint {
			Unknown = 0,

			GetSwapChainImages,
			AcquireNextImage,
			BeginCommandBuffer,
			EndCommandBuffer,

			// Create
			CreateSwapChain,
			CreateDescriptorSetLayout,
			CreateDescriptorPool,
			CreateImage,
			CreateImageView,
			CreateImageViews,
			CreateBuffer,
			CreateInstance,
			CreateDebugMessenger,
			CreateShaderModule,
			CreatePipelineLayout,
			CreateGraphicsPipeline,
			CreateSemaphore,
			CreateFence,
			CreateCommandPool,
			CreateSurface,
			CreateLogicalDevice,
			CreateTextureSampler,

			// Allocate
			AllocateDescriptorSets,
			AllocateCommandBuffer,
			AllocateCommandBuffers,
			AllocateMemory,

			// Queue
			QueueSubmit,
			QueuePresent,

			// Bind
			BindBufferMemory,
			BindImageMemory,
		}
	}
}