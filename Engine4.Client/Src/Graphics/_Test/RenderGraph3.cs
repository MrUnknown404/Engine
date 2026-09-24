using System.Diagnostics.CodeAnalysis;
using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Graphics.Vulkan.Objects;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics._Test;

public sealed unsafe class RenderGraph3 {
	// TODO clean up & optimize once working
	// TODO should i be using semaphores or pipeline barriers?
	//   vulkan tut uses both. i should probably use both

	private readonly VulkanRenderer vulkanRenderer;
	private readonly VulkanResourceManager resourceManager;

	// dynamic
	private readonly OrderedDictionary<RenderPassHandle, RenderPass3> renderPasses = new();
	private readonly Dictionary<RenderGraph.IResourceHandle, ResourceCreationData> resourcesToMake = new();

	// cached
	private readonly Dictionary<RenderGraph.IResourceHandle, ResourceData> cachedResources = new();
	private RenderPass3[] cachedSortedPasses = Array.Empty<RenderPass3>();
	private bool isDirty;

	internal RenderGraph3(VulkanRenderer vulkanRenderer, VulkanResourceManager resourceManager) {
		this.vulkanRenderer = vulkanRenderer;
		this.resourceManager = resourceManager;
	}

	[MustUseReturnValue]
	public BufferHandle AddBuffer(string resourceName, ulong size, VkBufferUsageFlagBits2 usageFlags, VkBufferCreateFlagBits createFlags, VkMemoryPropertyFlagBits memoryPropertyFlags) {
		BufferHandle handle = new(resourceName);
		if (!resourcesToMake.TryAdd(handle, new BufferCreationData(handle, size, usageFlags, createFlags, memoryPropertyFlags))) { throw new Exception(); } // TODO exception

		isDirty = true;
		return handle;
	}

	[MustUseReturnValue]
	public TextureHandle AddTexture(string resourceName, ushort width, ushort height, VkFormat format, VkImageUsageFlagBits imageUsageFlags) {
		TextureHandle handle = new(resourceName);
		if (!resourcesToMake.TryAdd(handle, new TextureCreationData(handle))) { throw new Exception(); } // TODO exception

		isDirty = true;
		return handle;
	}

	[MustUseReturnValue]
	public BufferHandle AddPersistentBuffer(string resourceName, VulkanBuffer buffer) {
		BufferHandle handle = new(resourceName);
		if (!cachedResources.TryAdd(handle, new BufferResourceData(buffer) { Persistent = true, })) { throw new Exception(); } // TODO exception

		isDirty = true;
		return handle;
	}

	[MustUseReturnValue]
	public TextureHandle AddPersistentTexture(string resourceName, VulkanTexture texture) {
		TextureHandle handle = new(resourceName);
		if (!cachedResources.TryAdd(handle, new ImageResourceData(texture) { Persistent = true, })) { throw new Exception(); } // TODO exception

		isDirty = true;
		return handle;
	}

	public void RemoveBuffer(BufferHandle handle) {
		if (!cachedResources.Remove(handle, out ResourceData? resourceData)) { throw new Exception(); } // TODO exception

		// TODO mark resource unused/cleanup resource
		isDirty = true;
	}

	public void RemoveTexture(TextureHandle handle) {
		if (!cachedResources.Remove(handle, out ResourceData? resourceData)) { throw new Exception(); } // TODO exception

		// TODO mark resource unused/cleanup resource
		isDirty = true;
	}

	[MustUseReturnValue]
	public RenderPassHandle AddPass(string passName, RenderPass3 renderPass) {
		RenderPassHandle handle = new(passName);
		if (!renderPasses.TryAdd(handle, renderPass)) {
			throw new Exception(); // TODO exception
		}

		renderPass.RenderGraph = this;

		isDirty = true;
		return handle;
	}

	public void RemovePass(RenderPassHandle handle) {
		if (!renderPasses.Remove(handle)) { throw new Exception(); } // TODO exception

		// TODO go through resources and kill (which should also clean) any orphans
		isDirty = true;
	}

	public void SetPassEnabled(RenderPassHandle handle, bool enabled) {
		renderPasses[handle].Enabled = enabled;
		isDirty = true;
	}

	[MustUseReturnValue]
	private ResourceData GetResource(RenderGraph.IResourceHandle handle) => cachedResources[handle];

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
		CreateResources();
		CalculateResourceLifetimes();
		CreateBarriers();
	}

	internal void Render(GraphicsCommandBuffer commandBuffer) {
		GetPasses(out RenderPass3[] sortedPasses); // (valid/enabled)

		foreach (RenderPass3 renderPass in sortedPasses) {
			GraphicsRenderPass3? graphicsPass = renderPass as GraphicsRenderPass3;

			if (graphicsPass != null) {
				VkExtent2D extent = vulkanRenderer.SwapChain.Extent;

				// TODO are these graphics only?
				commandBuffer.CmdSetViewport(0, 0, extent.width, extent.height, 0, 1); // TODO configurable? right now i can't change the depth
				commandBuffer.CmdSetScissor(new(), extent); // TODO configurable?

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

	private static RenderPass3[] SortRenderPasses(List<RenderPass3> enabledPasses) {
		uint[] inDegrees = new uint[enabledPasses.Count];
		Queue<RenderPass3> queuedPasses = new();
		List<uint>[] dependencies = new List<uint>[enabledPasses.Count];
		Dictionary<RenderGraph.IResourceHandle, uint> resourceWriters = new();
		Dictionary<RenderPass3, uint> passToIndex = new(); // don't like this

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
			foreach (WriteData output in pass.Outputs) { resourceWriters.TryAdd(output.ResourceHandle, i); }
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

	private void CreateResources() {
		cachedResources.Clear(); // TODO clean?

		foreach ((RenderGraph.IResourceHandle handle, ResourceCreationData resourceCreationData) in resourcesToMake) {
			cachedResources.Add(handle, resourceCreationData switch { // TODO union?
					BufferCreationData bufferCreationData => new BufferResourceData(resourceManager.CreateBuffer(handle.Name, bufferCreationData.Size, bufferCreationData.UsageFlags, bufferCreationData.CreateFlags,
						bufferCreationData.MemoryPropertyFlags)),
					TextureCreationData textureCreationData => new ImageResourceData(resourceManager.CreateTexture(handle.Name)),
					_ => throw new ArgumentOutOfRangeException(nameof(resourceCreationData)),
			});
		}

		resourcesToMake.Clear();
	}

	private void CalculateResourceLifetimes() {
		for (uint passIndex = 0; passIndex < cachedSortedPasses.Length; passIndex++) {
			RenderPass3 pass = cachedSortedPasses[passIndex];

			foreach (ReadData read in pass.Inputs) {
				ResourceData resource = cachedResources[read.ResourceHandle];
				resource.FirstUsePass = uint.Min(resource.FirstUsePass, passIndex);
				resource.LastUsePass = uint.Max(resource.LastUsePass, passIndex);
			}

			foreach (WriteData write in pass.Outputs) {
				ResourceData resource = cachedResources[write.ResourceHandle];
				resource.FirstUsePass = uint.Min(resource.FirstUsePass, passIndex);
				resource.LastUsePass = uint.Max(resource.LastUsePass, passIndex);
			}
		}
	}

	private void CreateBarriers() {
		foreach (RenderPass3 renderPass in cachedSortedPasses) {
			List<VkImageMemoryBarrier2> imageBarriers = new();
			List<VkBufferMemoryBarrier2> bufferBarriers = new();

			// VkPipelineStageFlagBits2 imageSrcStage = default;
			// VkPipelineStageFlagBits2 imageDstStage = default;
			// VkPipelineStageFlagBits2 bufferSrcStage = default;
			// VkPipelineStageFlagBits2 bufferDstStage = default;

			foreach (ReadData read in renderPass.Inputs) {
				ResourceData resource = GetResource(read.ResourceHandle);

				switch (read) {
					case ImageReadData imageRead when resource is ImageResourceData imageResource: { // TODO union?
						bool needsBarrier = imageResource.CurrentLayout != imageRead.ExpectedLayout || resource.CurrentAccessMask != read.AccessMask;

						if (needsBarrier) {
							VkImageMemoryBarrier2 imageMemoryBarrier = new() {
									srcAccessMask = resource.CurrentAccessMask,
									dstAccessMask = read.AccessMask,
									srcStageMask = resource.CurrentStageMask,
									dstStageMask = read.StageMask,
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
									srcAccessMask = resource.CurrentAccessMask,
									dstAccessMask = read.AccessMask,
									srcStageMask = resource.CurrentStageMask,
									dstStageMask = read.StageMask,
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

	// classes/structs

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

		internal WriteData(RenderGraph.IResourceHandle resourceHandle) => ResourceHandle = resourceHandle;
	}

	public sealed class BufferReadData : ReadData {
		internal BufferReadData(BufferHandle resourceHandle, VkAccessFlagBits2 accessMask, VkPipelineStageFlagBits2 stageMask) : base(resourceHandle, accessMask, stageMask) { }
	}

	public sealed class ImageReadData : ReadData {
		public VkImageLayout ExpectedLayout { get; }

		internal ImageReadData(TextureHandle resourceHandle, VkAccessFlagBits2 accessMask, VkPipelineStageFlagBits2 stageMask, VkImageLayout expectedLayout) : base(resourceHandle, accessMask, stageMask) =>
				ExpectedLayout = expectedLayout;
	}

	public abstract class ResourceData {
		public uint FirstUsePass { get; internal set; }
		public uint LastUsePass { get; internal set; }
		public VkAccessFlagBits2 CurrentAccessMask { get; internal set; }
		public VkPipelineStageFlagBits2 CurrentStageMask { get; internal set; }
		public bool Persistent { get; init; } // TODO if this flag is enabled, don't clean

		// i could store dependents?

		// TODO bool & data ptr or something for uploading data?
	}

	public sealed class BufferResourceData : ResourceData {
		public VulkanBuffer Buffer { get; }

		internal BufferResourceData(VulkanBuffer buffer) => Buffer = buffer;
	}

	public sealed class ImageResourceData : ResourceData {
		public VulkanTexture Texture { get; }
		public VkImageLayout CurrentLayout { get; set; }

		internal ImageResourceData(VulkanTexture texture) => Texture = texture;
	}

	public abstract class ResourceCreationData {
		public RenderGraph.IResourceHandle Handle { get; }
		protected ResourceCreationData(RenderGraph.IResourceHandle handle) => Handle = handle;
	}

	public sealed class BufferCreationData : ResourceCreationData {
		public ulong Size { get; }
		public VkBufferUsageFlagBits2 UsageFlags { get; }
		public VkBufferCreateFlagBits CreateFlags { get; }
		public VkMemoryPropertyFlagBits MemoryPropertyFlags { get; }

		internal BufferCreationData(BufferHandle handle, ulong size, VkBufferUsageFlagBits2 usageFlags, VkBufferCreateFlagBits createFlags, VkMemoryPropertyFlagBits memoryPropertyFlags) : base(handle) {
			Size = size;
			UsageFlags = usageFlags;
			CreateFlags = createFlags;
			MemoryPropertyFlags = memoryPropertyFlags;
		}
	}

	public sealed class TextureCreationData : ResourceCreationData {
		public ushort Width { get; }
		public ushort Height { get; }
		public VkFormat Format { get; }
		public VkImageUsageFlagBits ImageUsageFlags { get; }
		public VkImageLayout OldLayout { get; }
		public VkImageLayout NewLayout { get; }

		internal TextureCreationData(TextureHandle handle) : base(handle) { }
	}
}