using System.Diagnostics.CodeAnalysis;
using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Graphics.Vulkan.Objects;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics._Test;

public sealed unsafe class RenderGraph3 {
	private readonly VulkanRenderer vulkanRenderer;
	private readonly VulkanResourceManager resourceManager;

	//
	private readonly OrderedDictionary<RenderPassHandle, RenderPass3> renderPasses = new();
	private readonly Dictionary<RenderGraph.IResourceHandle, ResourceCreationData> resourcesToMake = new();
	private readonly Dictionary<RenderGraph.IResourceHandle, ResourceData> resources = new();

	// cached
	private RenderPass3[] cachedSortedPasses = Array.Empty<RenderPass3>();
	private bool isDirty;

	internal RenderGraph3(VulkanRenderer vulkanRenderer, VulkanResourceManager resourceManager) {
		this.vulkanRenderer = vulkanRenderer;
		this.resourceManager = resourceManager;
	}

	[MustUseReturnValue]
	public BufferHandle AddBuffer(string resourceName, ulong bufferSize, VkBufferUsageFlagBits2 bufferUsageFlags) {
		BufferHandle handle = new(resourceName);
		if (resourcesToMake.TryAdd(handle, new BufferCreationData(handle, bufferSize, bufferUsageFlags))) {
			isDirty = true;
			return handle;
		}

		throw new Exception(); // TODO exception
	}

	[MustUseReturnValue]
	public TextureHandle AddTexture(string resourceName, ushort width, ushort height, VkFormat format, VkImageUsageFlagBits imageUsageFlags) {
		TextureHandle handle = new(resourceName);
		if (resourcesToMake.TryAdd(handle, new TextureCreationData(handle))) {
			isDirty = true;
			return handle;
		}

		throw new Exception(); // TODO exception
	}

	public void RemoveBuffer(BufferHandle handle) => throw new NotImplementedException(); // TODO
	public void RemoveTexture(TextureHandle handle) => throw new NotImplementedException(); // TODO

	[MustUseReturnValue]
	public RenderPassHandle AddPass(string passName, RenderPass3 renderPass) {
		RenderPassHandle handle = new(passName);
		if (renderPasses.TryAdd(handle, renderPass)) {
			isDirty = true;
			return handle;
		}

		throw new Exception(); // TODO exception
	}

	public void RemovePass(RenderPassHandle handle) => throw new NotImplementedException(); // TODO (also remove resources)

	public void DisablePass(RenderPassHandle handle) {
		renderPasses[handle].Enabled = false;
		isDirty = true;
	}

	public void EnablePass(RenderPassHandle handle) {
		renderPasses[handle].Enabled = true;
		isDirty = true;
	}

	[MustUseReturnValue]
	private ResourceData GetResource(RenderGraph.IResourceHandle handle) => resources[handle];

	[MustUseReturnValue]
	public VulkanBuffer GetBuffer(BufferHandle handle) => (GetResource(handle) as BufferResourceData ?? throw new InvalidCastException()).Buffer;

	[MustUseReturnValue]
	public VulkanTexture GetTexture(TextureHandle handle) => (GetResource(handle) as ImageResourceData ?? throw new InvalidCastException()).Texture;

	private void Compile(IEnumerable<RenderPass3> allRenderPasses, out RenderPass3[] outSortedRenderPasses) {
		// this should make vulkan resources

		List<RenderPass3> enabledPasses = new();
		foreach (RenderPass3 renderPass in allRenderPasses) {
			if (renderPass.Enabled) { enabledPasses.Add(renderPass); }
		}

		outSortedRenderPasses = SortRenderPasses(enabledPasses);
		CreateResources(resourceManager, outSortedRenderPasses, resourcesToMake, resources);
		CreateBarriers(outSortedRenderPasses, GetResource);

		return;

		static RenderPass3[] SortRenderPasses(List<RenderPass3> enabledPasses) {
			uint[] inDegrees = new uint[enabledPasses.Count];
			Queue<RenderPass3> queuedPasses = new();
			List<uint>[] dependencies = new List<uint>[enabledPasses.Count];
			Dictionary<RenderGraph.IResourceHandle, uint> resourceWriters = new();
			Dictionary<RenderPass3, uint> passToIndex = new();

			// calc dependencies
			for (uint i = 0; i < enabledPasses.Count; i++) {
				RenderPass3 pass = enabledPasses[(int)i];
				passToIndex[pass] = i;
				dependencies[i] = new();

				// inputs
				foreach (ReadData input in pass.Inputs) {
					if (resourceWriters.TryGetValue(input.ResourceHandle, out uint value)) { dependencies[i].Add(value); }
				}

				// outputs
				foreach (WriteData output in pass.Outputs) { resourceWriters.Add(output.ResourceHandle, i); }
			}

			// calc inDegrees
			for (uint i = 0; i < enabledPasses.Count; i++) {
				foreach (uint passIndex in dependencies[i]) { inDegrees[passIndex]++; }
			}

			// enqueue 0 inDegree passes
			for (uint i = 0; i < enabledPasses.Count; i++) {
				if (inDegrees[i] == 0) { queuedPasses.Enqueue(enabledPasses[(int)i]); }
			}

			// sort passes
			List<RenderPass3> sortedPasses = new();
			while (queuedPasses.TryDequeue(out RenderPass3? renderPass)) {
				sortedPasses.Add(renderPass);

				foreach (uint dependency in dependencies[passToIndex[renderPass]]) {
					if (--inDegrees[(int)dependency] == 0) { queuedPasses.Enqueue(renderPass); }
				}
			}

			return sortedPasses.Count == enabledPasses.Count ? sortedPasses.ToArray() : throw new Exception(); // TODO exception
		}

		static void CreateResources(VulkanResourceManager resourceManager, RenderPass3[] sortedRenderPasses, Dictionary<RenderGraph.IResourceHandle, ResourceCreationData> resourcesToMake,
			Dictionary<RenderGraph.IResourceHandle, ResourceData> resources) {
			resources.Clear(); // TODO clean?

			foreach ((RenderGraph.IResourceHandle handle, ResourceCreationData resourceCreationData) in resourcesToMake) {
				resources.Add(handle, resourceCreationData switch { // TODO union?
						BufferCreationData bufferCreationData => new BufferResourceData(resourceManager.CreateBuffer(handle.Name, bufferCreationData.BufferSize, bufferCreationData.BufferUsageFlags)),
						TextureCreationData textureCreationData => new ImageResourceData(resourceManager.CreateTexture(handle.Name)),
						_ => throw new ArgumentOutOfRangeException(nameof(resourceCreationData)),
				});
			}

			CalculateResourceLifetimes(sortedRenderPasses, resources);
		}

		static void CalculateResourceLifetimes(RenderPass3[] sortedRenderPasses, Dictionary<RenderGraph.IResourceHandle, ResourceData> resources) {
			for (uint passIndex = 0; passIndex < sortedRenderPasses.Length; passIndex++) {
				RenderPass3 pass = sortedRenderPasses[passIndex];

				foreach (ReadData read in pass.Inputs) {
					ResourceData resource = resources[read.ResourceHandle];
					resource.FirstUsePass = uint.Min(resource.FirstUsePass, passIndex);
					resource.LastUsePass = uint.Max(resource.LastUsePass, passIndex);
				}

				foreach (WriteData write in pass.Outputs) {
					ResourceData resource = resources[write.ResourceHandle];
					resource.FirstUsePass = uint.Min(resource.FirstUsePass, passIndex);
					resource.LastUsePass = uint.Max(resource.LastUsePass, passIndex);
				}
			}
		}

		static void CreateBarriers(RenderPass3[] sortedRenderPasses, Func<RenderGraph.IResourceHandle, ResourceData> getResource) {
			foreach (RenderPass3 renderPass in sortedRenderPasses) {
				List<VkImageMemoryBarrier2> imageBarriers = new();
				List<VkBufferMemoryBarrier2> bufferBarriers = new();

				// VkPipelineStageFlagBits2 imageSrcStage = default;
				// VkPipelineStageFlagBits2 imageDstStage = default;
				// VkPipelineStageFlagBits2 bufferSrcStage = default;
				// VkPipelineStageFlagBits2 bufferDstStage = default;

				foreach (ReadData read in renderPass.Inputs) {
					ResourceData resource = getResource(read.ResourceHandle);

					switch (read) {
						case ImageReadData imageRead when resource is ImageResourceData imageResource: { // TODO union?
							bool needsBarrier = imageResource.CurrentLayout != imageRead.ExpectedLayout || resource.CurrentAccessMask != read.AccessMask;

							if (needsBarrier) {
								VkImageMemoryBarrier2 imageMemoryBarrier = new() {
										srcAccessMask = resource.CurrentAccessMask, //
										dstAccessMask = read.AccessMask,
										srcQueueFamilyIndex = Vk.QueueFamilyIgnored,
										dstQueueFamilyIndex = Vk.QueueFamilyIgnored,
										image = imageResource.Texture.Image,
										subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
										oldLayout = imageResource.CurrentLayout,
										newLayout = imageRead.ExpectedLayout,
								};

								imageBarriers.Add(imageMemoryBarrier);
								// imageSrcStage |= resource.CurrentStageMask;
								// imageDstStage |= read.StageMask;

								imageResource.CurrentLayout = imageRead.ExpectedLayout;
								imageResource.CurrentAccessMask = read.AccessMask;
								imageResource.CurrentStageMask = read.StageMask;
							}

							break;
						}
						case BufferReadData bufferRead when resource is BufferResourceData bufferResource: {
							bool needsBarrier = resource.CurrentAccessMask != read.AccessMask;

							if (needsBarrier) {
								VkBufferMemoryBarrier2 bufferMemoryBarrier = new() {
										srcAccessMask = resource.CurrentAccessMask, //
										dstAccessMask = read.AccessMask,
										srcQueueFamilyIndex = Vk.QueueFamilyIgnored,
										dstQueueFamilyIndex = Vk.QueueFamilyIgnored,
										buffer = bufferResource.Buffer.VkBuffer,
										size = Vk.WholeSize, // should i do this? or set per resource?
										offset = 0, // ^
								};

								bufferBarriers.Add(bufferMemoryBarrier);
								// bufferSrcStage |= resource.CurrentStageMask;
								// bufferDstStage |= read.StageMask;

								bufferResource.CurrentAccessMask = read.AccessMask;
								bufferResource.CurrentStageMask = read.StageMask;
							}

							break;
						}
						default: throw new ArgumentOutOfRangeException();
					}
				}

				renderPass.BufferMemoryBarriers = bufferBarriers.ToArray();
				renderPass.ImageMemoryBarriers = imageBarriers.ToArray();
			}
		}
	}

	internal void Render(GraphicsCommandBuffer commandBuffer) {
		GetPasses(out RenderPass3[] sortedPasses); // (valid/enabled)

		foreach (RenderPass3 renderPass in sortedPasses) {
			GraphicsRenderPass3? graphicsPass = renderPass as GraphicsRenderPass3;

			if (graphicsPass != null) {
				VkExtent2D extent = vulkanRenderer.SwapChain.Extent;

				commandBuffer.CmdSetViewport(0, 0, extent.width, extent.height, 0, 1); // TODO configurable? right now i can't change the depth
				commandBuffer.CmdSetScissor(0, 0, extent); // TODO configurable?

				/* TODO more?
					commandBuffer.CmdBindGraphicsPipeline();
					commandBuffer.CmdBindVertexBuffer();
					commandBuffer.CmdBindIndexBuffer();
				*/

				VkRenderingAttachmentInfo colorAttachment;
				VkRenderingAttachmentInfo depthAttachment;

				if (graphicsPass.ClearColor is { } clearColor) {
					colorAttachment = new() {
							imageView = vulkanRenderer.GetSwapChainImageView(),
							imageLayout = VkImageLayout.ImageLayoutAttachmentOptimalKhr,
							loadOp = VkAttachmentLoadOp.AttachmentLoadOpClear,
							storeOp = VkAttachmentStoreOp.AttachmentStoreOpStore,
							clearValue = new() { color = clearColor, },
					};
				}

				if (graphicsPass is { DepthImageHandle: { } depthImageHandle, DepthStencil: { } depthStencil, }) {
					depthAttachment = new() {
							imageView = GetTexture(depthImageHandle).View,
							imageLayout = VkImageLayout.ImageLayoutAttachmentOptimalKhr,
							loadOp = VkAttachmentLoadOp.AttachmentLoadOpClear,
							storeOp = VkAttachmentStoreOp.AttachmentStoreOpStore,
							clearValue = new() { depthStencil = depthStencil, },
					};
				}

				VkRenderingInfo renderingInfo = new() {
						renderArea = new() { offset = new(0, 0), extent = extent, }, // TODO allow sizes that aren't related to the swap chain
						layerCount = 1,
						colorAttachmentCount = graphicsPass.ClearColor != null ? 1u : 0u,
						pColorAttachments = graphicsPass.ClearColor != null ? &colorAttachment : null,
						pDepthAttachment = graphicsPass.DepthStencil != null ? &depthAttachment : null,
				};

				commandBuffer.CmdBeginRendering(renderingInfo);
			}

			TryInsertBarriers(commandBuffer, renderPass.BufferMemoryBarriers, renderPass.ImageMemoryBarriers);
			// more?

			renderPass.Execute(commandBuffer);

			if (graphicsPass != null) {
				commandBuffer.CmdEndRendering(); //
			}
		}

		return;

		void GetPasses(out RenderPass3[] sortedPasses) {
			if (isDirty) {
				Compile(renderPasses.Values, out cachedSortedPasses);
				isDirty = false;
			}

			sortedPasses = cachedSortedPasses;
		}

		void TryInsertBarriers(GraphicsCommandBuffer commandBuffer, VkBufferMemoryBarrier2[] bufferBarriers, VkImageMemoryBarrier2[] imageBarriers) {
			// List<VkBufferMemoryBarrier2> bufferBarriersList = new(); // TODO compile this?
			// List<VkImageMemoryBarrier2> imageBarriersList = new();
			//
			// foreach (RenderGraph.IResourceHandle handle in renderPass.Inputs) {
			// 	switch (handle) { // TODO union?
			// 		case BufferHandle bufferHandle:
			// 			bufferBarriersList.Add(new() {
			// 					srcAccessMask = VkAccessFlagBits2.Access2MemoryWriteBit,
			// 					dstAccessMask = VkAccessFlagBits2.Access2ShaderReadBit,
			// 					srcQueueFamilyIndex = Vk.QueueFamilyIgnored,
			// 					dstQueueFamilyIndex = Vk.QueueFamilyIgnored,
			// 					buffer = GetBuffer(bufferHandle).VkBuffer,
			// 					size = Vk.WholeSize, // TODO should i do this? or set per resource?
			// 					offset = 0, // ^
			// 			}); break;
			// 		case TextureHandle textureHandle:
			// 			imageBarriersList.Add(new() {
			// 					srcAccessMask = VkAccessFlagBits2.Access2MemoryWriteBit,
			// 					dstAccessMask = VkAccessFlagBits2.Access2ShaderReadBit,
			// 					srcQueueFamilyIndex = Vk.QueueFamilyIgnored,
			// 					dstQueueFamilyIndex = Vk.QueueFamilyIgnored,
			// 					oldLayout = imageResource.InitialLayout,
			// 					newLayout = VkImageLayout.ImageLayoutShaderReadOnlyOptimal,
			// 					image = GetTexture(textureHandle).Image,
			// 					subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
			//
			// 					//
			// 			}); break;
			// 		default: throw new ArgumentOutOfRangeException();
			// 	}
			// }
			//
			// foreach (RenderGraph.IResourceHandle handle in renderPass.Outputs) {
			// 	switch (handle) { // TODO union?
			// 		case BufferHandle bufferHandle:
			// 			bufferBarriersList.Add(new() {
			// 					srcAccessMask = VkAccessFlagBits2.Access2MemoryReadBit,
			// 					dstAccessMask = VkAccessFlagBits2.Access2ColorAttachmentWriteBit,
			// 					srcQueueFamilyIndex = Vk.QueueFamilyIgnored,
			// 					dstQueueFamilyIndex = Vk.QueueFamilyIgnored,
			// 					buffer = GetBuffer(bufferHandle).VkBuffer,
			// 					size = Vk.WholeSize, // TODO should i do this? or set per resource?
			// 					offset = 0, // ^
			// 			}); break;
			// 		case TextureHandle textureHandle:
			// 			imageBarriersList.Add(new() {
			// 					srcAccessMask = VkAccessFlagBits2.Access2MemoryReadBit,
			// 					dstAccessMask = VkAccessFlagBits2.Access2ColorAttachmentWriteBit,
			// 					srcQueueFamilyIndex = Vk.QueueFamilyIgnored,
			// 					dstQueueFamilyIndex = Vk.QueueFamilyIgnored,
			// 					oldLayout = imageResource.InitialLayout,
			// 					newLayout = VkImageLayout.ImageLayoutShaderReadOnlyOptimal,
			// 					image = GetTexture(textureHandle).Image,
			// 					subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
			//
			// 			}); break;
			// 		default: throw new ArgumentOutOfRangeException();
			// 	}
			// }

			//

			if (bufferBarriers.Length == 0 && imageBarriers.Length == 0) { return; }

			fixed (VkBufferMemoryBarrier2* pBufferBarriers = bufferBarriers) {
				fixed (VkImageMemoryBarrier2* pImageBarriers = imageBarriers) {
					VkDependencyInfo dependencyInfo = new() {
							dependencyFlags = VkDependencyFlagBits.DependencyByRegionBit, // what does this flag do?
					};

					if (bufferBarriers.Length != 0) { dependencyInfo = dependencyInfo with { bufferMemoryBarrierCount = (uint)bufferBarriers.Length, pBufferMemoryBarriers = pBufferBarriers, }; }
					if (imageBarriers.Length != 0) { dependencyInfo = dependencyInfo with { imageMemoryBarrierCount = (uint)imageBarriers.Length, pImageMemoryBarriers = pImageBarriers, }; }

					commandBuffer.CmdPipelineBarrier(dependencyInfo);
				}
			}
		}
	}

	public readonly record struct RenderPassHandle {
		public required string Name { get; init; }

		[SetsRequiredMembers]
		public RenderPassHandle(string name) => Name = name;
	}

	public readonly record struct BufferHandle : RenderGraph.IResourceHandle {
		public required string Name { get; init; }

		[SetsRequiredMembers]
		public BufferHandle(string name) => Name = name;
	}

	public readonly record struct TextureHandle : RenderGraph.IResourceHandle {
		public required string Name { get; init; }

		[SetsRequiredMembers]
		public TextureHandle(string name) => Name = name;
	}

	public abstract class ReadData {
		public RenderGraph.IResourceHandle ResourceHandle { get; }
		public VkAccessFlagBits2 AccessMask { get; }
		public VkPipelineStageFlagBits2 StageMask { get; }

		protected ReadData(RenderGraph.IResourceHandle resourceHandle, VkAccessFlagBits2 accessMask, VkPipelineStageFlagBits2 stageMask) {
			ResourceHandle = resourceHandle;
			AccessMask = accessMask;
			StageMask = stageMask;
		}
	}

	public class WriteData {
		public RenderGraph.IResourceHandle ResourceHandle { get; }

		public WriteData(RenderGraph.IResourceHandle resourceHandle) => ResourceHandle = resourceHandle;
	}

	public sealed class BufferReadData : ReadData {
		public BufferReadData(BufferHandle resourceHandle, VkAccessFlagBits2 accessMask, VkPipelineStageFlagBits2 stageMask) : base(resourceHandle, accessMask, stageMask) { }
	}

	public sealed class ImageReadData : ReadData {
		public VkImageLayout ExpectedLayout { get; }

		public ImageReadData(TextureHandle resourceHandle, VkAccessFlagBits2 accessMask, VkPipelineStageFlagBits2 stageMask, VkImageLayout expectedLayout) : base(resourceHandle, accessMask, stageMask) =>
				ExpectedLayout = expectedLayout;
	}

	public abstract class ResourceData {
		public uint FirstUsePass { get; internal set; }
		public uint LastUsePass { get; internal set; }
		public VkAccessFlagBits2 CurrentAccessMask { get; internal set; }
		public VkPipelineStageFlagBits2 CurrentStageMask { get; internal set; }
	}

	public sealed class BufferResourceData : ResourceData {
		public VulkanBuffer Buffer { get; }

		public BufferResourceData(VulkanBuffer buffer) => Buffer = buffer;
	}

	public sealed class ImageResourceData : ResourceData {
		public VulkanTexture Texture { get; }
		public VkImageLayout CurrentLayout { get; set; }

		public ImageResourceData(VulkanTexture texture) => Texture = texture;
	}

	public abstract class ResourceCreationData {
		public RenderGraph.IResourceHandle Handle { get; }
		protected ResourceCreationData(RenderGraph.IResourceHandle handle) => Handle = handle;
	}

	public sealed class BufferCreationData : ResourceCreationData {
		public ulong BufferSize { get; }
		public VkBufferUsageFlagBits2 BufferUsageFlags { get; }

		public BufferCreationData(BufferHandle handle, ulong bufferSize, VkBufferUsageFlagBits2 bufferUsageFlags) : base(handle) {
			BufferSize = bufferSize;
			BufferUsageFlags = bufferUsageFlags;
		}
	}

	public sealed class TextureCreationData : ResourceCreationData {
		public TextureCreationData(TextureHandle handle) : base(handle) { }
	}
}