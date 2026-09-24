using System.Diagnostics.CodeAnalysis;
using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Graphics.Vulkan.Objects;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;
using Semaphore = Engine4.Client.Graphics.Vulkan.Objects.Semaphore;

namespace Engine4.Client.Graphics._Test;

// TODO look into
//  https://en.wikipedia.org/wiki/Directed_acyclic_graph
//  https://en.wikipedia.org/wiki/Topological_sorting#Kahn's_algorithm
//  https://www.geeksforgeeks.org/dsa/introduction-to-directed-acyclic-graph/
//  https://www.geeksforgeeks.org/dsa/topological-sorting-indegree-based-solution/
//  https://www.gdcvault.com/play/1024612/FrameGraph-Extensible-Rendering-Architecture-in
//  https://themaister.net/blog/2017/08/15/render-graphs-and-vulkan-a-deep-dive/
//  https://apoorvaj.io/render-graphs-1
//  https://vkguide.dev/docs/ascendant/ascendant_light/
//  https://dev.to/p3ngu1nzz/advanced-vulkan-rendering-building-a-modern-frame-graph-and-memory-management-system-15kn
//  https://docs.vulkan.org/tutorial/latest/Building_a_Simple_Engine/Engine_Architecture/05_rendering_pipeline.html
//  https://alielmorsy.github.io/the-art-of-render-graphs/
//  https://deepwiki.com/inexorgame/vulkan-renderer/4-render-graph
//  https://poniesandlight.co.uk/reflect/island_rendergraph_1/
//  https://poniesandlight.co.uk/reflect/island_rendergraph_2/
//  https://github.com/asc-community/VulkanAbstractionLayer/tree/master
//  https://godotengine.org/article/rendering-acyclic-graph/
// TODO document this when done

public unsafe class RenderGraph {
	// TODO i could maybe optimize this by switching to a list? idk how fast OrderedDictionary is. use a dict of handle -> index to access the list. i want the handle so i can access passes to edit
	private readonly OrderedDictionary<RenderPassHandle, RenderPass2> renderPasses = new();
	// TODO add support for static resources? they would be set at the beginning of the program's lifetime and never change
	private readonly Dictionary<IResourceHandle, ResourceData> resources = new(); // TODO how efficient is using a string as a hash code? should i make my own type? it's probably fine?
	private readonly VulkanResourceManager resourceManager;

	private bool isDirty;

	// compiled
	private RenderPass2[] bakedRenderPasses = Array.Empty<RenderPass2>();
	private uint[] passExecutionOrder = Array.Empty<uint>();
	private Semaphore[] semaphores = Array.Empty<Semaphore>();
	private (uint, uint)[] semaphoreSignalWaitPairs = Array.Empty<(uint, uint)>();

	internal RenderGraph(VulkanResourceManager resourceManager) => this.resourceManager = resourceManager;

	[MustUseReturnValue]
	public BufferHandle AddBuffer(string resourceName, ulong size, VkBufferUsageFlagBits2 usageFlags, VkBufferCreateFlagBits createFlags, VkMemoryPropertyFlagBits memoryPropertyFlags) { // TODO properties
		BufferHandle handle = new(resourceName);
		if (!resources.TryGetValue(handle, out ResourceData? resourceData)) {
			resourceData = new BufferResourceData(resourceManager, resourceName, size, usageFlags, createFlags, memoryPropertyFlags);
			resources.Add(handle, resourceData);
			return handle;
		} else { throw new Exception(); } // TODO exception
	}

	[MustUseReturnValue]
	public ImageHandle AddTexture(string resourceName, ushort width, ushort height, VkFormat format, VkImageUsageFlagBits imageUsageFlags, VkImageLayout layout) { // TODO properties
		ImageHandle handle = new(resourceName);
		if (!resources.TryGetValue(handle, out ResourceData? resourceData)) {
			resourceData = new ImageResourceData(resourceManager, resourceName, format, new(width, height), imageUsageFlags, VkImageLayout.ImageLayoutUndefined, layout);
			resources.Add(handle, resourceData);
			return handle;
		} else { throw new Exception(); } // TODO exception
	}

	public void RemoveBuffer(string resourceName) => throw new NotImplementedException(); // TODO
	public void RemoveTexture(string resourceName) => throw new NotImplementedException(); // TODO

	public RenderPassHandle AddPass([HandlesResourceDisposal] RenderPassBuilder builder) {
		if (builder.Invalid) { throw new Exception(); } // TODO exception

		RenderPass2 renderPass = new(builder);
		builder.Dispose();

		renderPasses.Add(renderPass.Handle, renderPass);
		isDirty = true;
		return renderPass.Handle;
	}

	public void RemovePass(RenderPassHandle handle) {
		renderPasses.Remove(handle);
		isDirty = true;

		// TODO clean up any resources no longer in use
	}

	public void DisablePass(RenderPassHandle handle) {
		renderPasses[handle].Enabled = false;
		isDirty = true;
	}

	public void EnablePass(RenderPassHandle handle) {
		renderPasses[handle].Enabled = true;
		isDirty = true;
	}

	// effectively https://docs.vulkan.org/tutorial/latest/Building_a_Simple_Engine/Engine_Architecture/05_rendering_pipeline.html#_rendergraph_dependency_analysis_and_execution_ordering
	// TODO doc
	private static void BakeGraph(VulkanResourceManager resourceManager, Dictionary<IResourceHandle, ResourceData> resources, IEnumerable<RenderPass2> inRenderPasses, out RenderPass2[] outRenderPasses,
		out uint[] outPassExecutionOrder, out Semaphore[] outSemaphores, out (uint, uint)[] outSemaphoreSignalWaitPairs) {
		List<RenderPass2> enabledRenderPasses = new();
		foreach (RenderPass2 renderPass in inRenderPasses) {
			if (renderPass.Enabled) { enabledRenderPasses.Add(renderPass); }
		}

		uint passCount = (uint)enabledRenderPasses.Count;
		List<uint>[] dependencies = new List<uint>[passCount];
		List<uint>[] dependents = new List<uint>[passCount];
		Dictionary<IResourceHandle, uint> resourceWriters = new();
		List<RenderPass2> renderPasses = new();
		List<uint> passExecutionOrder = new();

		for (uint i = 0; i < passCount; i++) {
			RenderPass2 pass = enabledRenderPasses[(int)i];

			// inputs
			foreach (IResourceHandle input in pass.Inputs) {
				if (resourceWriters.TryGetValue(input, out uint value)) {
					dependencies[i].Add(value);
					dependents[value].Add(i);
				}
			}

			// outputs
			foreach (IResourceHandle output in pass.Outputs) { resourceWriters.Add(output, i); }
		}

		bool[] visited = new bool[passCount];
		bool[] inStack = new bool[passCount];

		for (uint i = 0; i < passCount; i++) {
			if (!visited[i]) { Visit(i); }
		}

		outRenderPasses = renderPasses.ToArray();
		outPassExecutionOrder = passExecutionOrder.ToArray();
		CreateResources(resourceManager, resources, out outSemaphores, out outSemaphoreSignalWaitPairs);

		// TODO create pipeline

		return;

		void Visit(uint passIndex) {
			if (inStack[passIndex]) { throw new Exception(); } // TODO exception
			if (visited[passIndex]) { throw new Exception(); } // TODO exception

			inStack[passIndex] = true;

			foreach (uint dependent in dependents[passIndex]) { Visit(dependent); }

			inStack[passIndex] = false;
			visited[passIndex] = true;
			renderPasses.Add(enabledRenderPasses[(int)passIndex]);
			passExecutionOrder.Add(passIndex);
		}

		void CreateResources(VulkanResourceManager resourceManager, Dictionary<IResourceHandle, ResourceData> resources, out Semaphore[] outSemaphores, out (uint, uint)[] outSemaphoreSignalWaitPairs) {
			List<Semaphore> semaphores = new();
			List<(uint, uint)> semaphoreSignalWaitPairs = new();

			for (uint i = 0; i < passCount; i++) {
				foreach (uint dependency in dependencies[i]) {
					semaphores.Add(resourceManager.CreateSemaphore($"RenderGraph Pass[{i}] ({dependency})", 0));
					semaphoreSignalWaitPairs.Add(new(dependency, i));
				}
			}

			foreach ((IResourceHandle resourceHandle, ResourceData data) in resources) {
				switch (data) { } // TODO create resources. buffers/textures
				throw new NotImplementedException();
			}

			outSemaphores = semaphores.ToArray();
			outSemaphoreSignalWaitPairs = semaphoreSignalWaitPairs.ToArray();
		}

		/*
		Dictionary<RenderPass2, RenderPass2[]> edges = CalculateEdges(renderPasses, resources); // calculate edges
		if (edges.Count == 0) { return Array.Empty<RenderPass2>(); } // nothing to draw

		Dictionary<RenderPass2, uint> inDegrees = CalculateInDegrees(edges.Values); // also functions as what we have left to calculate
		Queue<RenderPass2> passQueue = new();
		List<RenderPass2> visited = new();

		// add all passes without dependencies to start
		foreach ((RenderPass2 pass, uint inDegree) in inDegrees) {
			if (inDegree == 0) { passQueue.Enqueue(pass); }
		}

		// process passes
		List<RenderPass2> output = new();
		while (passQueue.TryDequeue(out RenderPass2? renderPass)) { // renderPass should have no dependencies
			visited.Add(renderPass);
			output.Add(renderPass);

			if (edges.TryGetValue(renderPass, out RenderPass2[]? connectedArray)) {
				if (connectedArray == null) { throw new NullReferenceException(); }

				foreach (RenderPass2 connected in connectedArray) {
					uint inDegree = inDegrees[connected] - 1;
					inDegrees[connected] = inDegree;
					if (inDegree == 0) { passQueue.Enqueue(connected); } // does this work?
				}
			}
		}

		// check valid
		foreach (RenderPass2 renderPass in inDegrees.Keys) {
			if (!visited.Contains(renderPass)) { throw new Engine4Exception("Failed to compile render graph. Did not visit some nodes"); }
		}

		return output.ToArray();

		[MustUseReturnValue]
		static Dictionary<RenderPass2, RenderPass2[]> CalculateEdges(Dictionary<RenderPassHandle, RenderPass2> renderPasses, Dictionary<string, ResourceData> resources) => throw new NullReferenceException();

		[MustUseReturnValue]
		static Dictionary<RenderPass2, uint> CalculateInDegrees(IEnumerable<RenderPass2[]> edges) {
			Dictionary<RenderPass2, uint> inDegrees = new();

			foreach (RenderPass2[] edgeRenderPasses in edges) {
				foreach (RenderPass2 edgeRenderPass in edgeRenderPasses) {
					inDegrees[edgeRenderPass] = inDegrees.TryGetValue(edgeRenderPass, out uint inDegree) ? inDegree + 1 : 0; // add one
				}
			}

			return inDegrees;
		}
		*/
	}

	internal void Render(GraphicsCommandBuffer graphicsCommandBuffer, VkQueue graphicsQueue, VkQueue transferQueue) {
		if (isDirty) { // move?
			BakeGraph(resourceManager, resources, renderPasses.Values, out bakedRenderPasses, out passExecutionOrder, out semaphores, out semaphoreSignalWaitPairs); // TODO how do i want to handle cleanup?
			isDirty = false;
		}

		// List<CommandBuffer> commandBuffers = new(); TODO why is this here?
		List<Semaphore> waitSemaphores = new();
		List<VkPipelineStageFlagBits> waitStages = new();
		List<Semaphore> signalSemaphores = new();

		// TODO upload data to buffers

		foreach (uint passIndex in passExecutionOrder) {
			RenderPass2 renderPass = bakedRenderPasses[passIndex];

			waitSemaphores.Clear(); // TODO do i need to clean these?
			waitStages.Clear();
			signalSemaphores.Clear();

			for (int i = 0; i < semaphoreSignalWaitPairs.Length; i++) {
				if (semaphoreSignalWaitPairs[i].Item1 == passIndex) {
					signalSemaphores.Add(semaphores[i]); //
				}

				if (semaphoreSignalWaitPairs[i].Item2 == passIndex) {
					waitSemaphores.Add(semaphores[i]);
					waitStages.Add(VkPipelineStageFlagBits.PipelineStageColorAttachmentOutputBit);
				}
			}

			// TODO bind buffers
			// TODO bind pipeline

			if (renderPass.RenderPassStage == RenderPassStage.Graphics) {
				// TODO begin/end?
				// graphicsCommandBuffer.CmdBeginRendering(extent, colorView, clearColor, depthView, depthStencil);
			}

			// graphicsCommandBuffer.BeginCommandBuffer(0); // TODO is this the correct begin?

			//
			foreach (IResourceHandle resourceHandle in renderPass.Inputs) { // TODO calculate these in BakeGraph()
				ResourceData resourceData = resources[resourceHandle]; // TODO union? method?
				switch (resourceData) {
					case BufferResourceData bufferResource:
						VkBufferMemoryBarrier2 bufferMemoryBarrier = new() {
								srcQueueFamilyIndex = Vk.QueueFamilyIgnored,
								dstQueueFamilyIndex = Vk.QueueFamilyIgnored,
								buffer = bufferResource.Buffer.VkBuffer,
								size = Vk.WholeSize, // should i do this? or set per resource?
								offset = 0, // ^
								srcAccessMask = VkAccessFlagBits2.Access2MemoryWriteBit,
								dstAccessMask = VkAccessFlagBits2.Access2ShaderReadBit,
								// srcStageMask = , add these?
								// dstStageMask = ,
						};

						// TODO set multiple at once?
						graphicsCommandBuffer.CmdPipelineBarrier(new() { bufferMemoryBarrierCount = 1, pBufferMemoryBarriers = &bufferMemoryBarrier, dependencyFlags = VkDependencyFlagBits.DependencyByRegionBit, });

						break;
					case ImageResourceData imageResource:
						VkImageMemoryBarrier2 imageMemoryBarrier = new() {
								oldLayout = imageResource.InitialLayout,
								newLayout = VkImageLayout.ImageLayoutShaderReadOnlyOptimal,
								srcQueueFamilyIndex = Vk.QueueFamilyIgnored,
								dstQueueFamilyIndex = Vk.QueueFamilyIgnored,
								image = imageResource.Texture.Image,
								subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
								srcAccessMask = VkAccessFlagBits2.Access2MemoryWriteBit,
								dstAccessMask = VkAccessFlagBits2.Access2ShaderReadBit,
								// srcStageMask = , add these?
								// dstStageMask = ,
						};

						// TODO set multiple at once?
						graphicsCommandBuffer.CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier, dependencyFlags = VkDependencyFlagBits.DependencyByRegionBit, });

						break;
				}
			}

			//
			foreach (IResourceHandle resourceHandle in renderPass.Outputs) {
				ResourceData resourceData = resources[resourceHandle]; // TODO union? method?
				switch (resourceData) {
					case BufferResourceData bufferResource:
						VkBufferMemoryBarrier2 bufferMemoryBarrier = new() {
								srcQueueFamilyIndex = Vk.QueueFamilyIgnored,
								dstQueueFamilyIndex = Vk.QueueFamilyIgnored,
								buffer = bufferResource.Buffer.VkBuffer,
								size = Vk.WholeSize, // should i do this? or set per resource?
								offset = 0, // ^
								srcAccessMask = VkAccessFlagBits2.Access2MemoryReadBit,
								dstAccessMask = VkAccessFlagBits2.Access2ColorAttachmentWriteBit,
						};

						// TODO set multiple at once?
						graphicsCommandBuffer.CmdPipelineBarrier(new() { bufferMemoryBarrierCount = 1, pBufferMemoryBarriers = &bufferMemoryBarrier, dependencyFlags = VkDependencyFlagBits.DependencyByRegionBit, });

						break;
					case ImageResourceData imageResource:
						VkImageMemoryBarrier2 imageMemoryBarrier = new() {
								oldLayout = imageResource.InitialLayout,
								newLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
								srcQueueFamilyIndex = Vk.QueueFamilyIgnored,
								dstQueueFamilyIndex = Vk.QueueFamilyIgnored,
								image = imageResource.Texture.Image,
								subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
								srcAccessMask = VkAccessFlagBits2.Access2MemoryReadBit,
								dstAccessMask = VkAccessFlagBits2.Access2ColorAttachmentWriteBit,
						};

						// TODO set multiple at once?
						graphicsCommandBuffer.CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier, dependencyFlags = VkDependencyFlagBits.DependencyByRegionBit, });

						break;
				}
			}

			// exec
			renderPass.Exec?.Invoke(graphicsCommandBuffer);

			//
			foreach (IResourceHandle resourceHandle in renderPass.Outputs) {
				ResourceData resourceData = resources[resourceHandle]; // TODO union? method?
				switch (resourceData) {
					case BufferResourceData bufferResource:
						VkBufferMemoryBarrier2 bufferMemoryBarrier = new() {
								srcQueueFamilyIndex = Vk.QueueFamilyIgnored,
								dstQueueFamilyIndex = Vk.QueueFamilyIgnored,
								buffer = bufferResource.Buffer.VkBuffer,
								size = Vk.WholeSize, // should i do this? or set per resource?
								offset = 0, // ^
								srcAccessMask = VkAccessFlagBits2.Access2ColorAttachmentWriteBit,
								dstAccessMask = VkAccessFlagBits2.Access2MemoryReadBit,
						};

						// TODO set multiple at once?
						graphicsCommandBuffer.CmdPipelineBarrier(new() { bufferMemoryBarrierCount = 1, pBufferMemoryBarriers = &bufferMemoryBarrier, dependencyFlags = VkDependencyFlagBits.DependencyByRegionBit, });

						break;
					case ImageResourceData imageResource:
						VkImageMemoryBarrier2 imageMemoryBarrier = new() {
								oldLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
								newLayout = imageResource.FinalLayout,
								srcQueueFamilyIndex = Vk.QueueFamilyIgnored,
								dstQueueFamilyIndex = Vk.QueueFamilyIgnored,
								image = imageResource.Texture.Image,
								subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
								srcAccessMask = VkAccessFlagBits2.Access2ColorAttachmentWriteBit,
								dstAccessMask = VkAccessFlagBits2.Access2MemoryReadBit,
						};

						// TODO set multiple at once?
						graphicsCommandBuffer.CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier, dependencyFlags = VkDependencyFlagBits.DependencyByRegionBit, });

						break;
				}
			}

			// graphicsCommandBuffer.EndCommandBuffer();

			if (renderPass.RenderPassStage == RenderPassStage.Graphics) { graphicsCommandBuffer.CmdEndRendering(); }

			VkSemaphore[] waitSemaphoresArray = new VkSemaphore[waitSemaphores.Count];
			for (int i = 0; i < waitSemaphoresArray.Length; i++) { waitSemaphoresArray[i] = waitSemaphores[i].VkSemaphore; }

			VkPipelineStageFlagBits[] waitStagesArray = new VkPipelineStageFlagBits[waitStages.Count];
			for (int i = 0; i < waitStagesArray.Length; i++) { waitStagesArray[i] = waitStages[i]; }

			VkSemaphore[] signalSemaphoresArray = new VkSemaphore[signalSemaphores.Count];
			for (int i = 0; i < signalSemaphoresArray.Length; i++) { signalSemaphoresArray[i] = signalSemaphores[i].VkSemaphore; }

			VkCommandBuffer commandBuffer = graphicsCommandBuffer.VkCommandBuffer;

			fixed (VkSemaphore* pWaitSemaphores = waitSemaphoresArray) {
				fixed (VkPipelineStageFlagBits* pWaitStages = waitStagesArray) {
					fixed (VkSemaphore* pSignalSemaphores = signalSemaphoresArray) {
						VkSubmitInfo submitInfo = new() {
								waitSemaphoreCount = (uint)waitSemaphores.Count,
								pWaitSemaphores = pWaitSemaphores,
								pWaitDstStageMask = pWaitStages,
								commandBufferCount = 1,
								pCommandBuffers = &commandBuffer,
								signalSemaphoreCount = (uint)signalSemaphores.Count,
								pSignalSemaphores = pSignalSemaphores,
						};

						// TODO queue object. queue for each renderPass.RenderPassStage? or always use graphics?
						Vk.QueueSubmit(renderPass.RenderPassStage switch {
								RenderPassStage.Compute or RenderPassStage.Graphics => graphicsQueue,
								RenderPassStage.Transfer => transferQueue,
								_ => throw new ArgumentOutOfRangeException(),
						}, 1, &submitInfo, VkFence.Zero);
					}
				}
			}
		}

		/*
		foreach (RenderPass2 renderPass in bakedRenderPasses) {
			// TODO upload data
			// TODO sync

			switch (renderPass.RenderPassStage) {
				case RenderPassStage.Compute: break; // TODO
				case RenderPassStage.Graphics: // TODO
					// CmdBeginRendering
					// CmdEndRendering
					break;
				case RenderPassStage.Transfer: break; // TODO
				default: throw new ArgumentOutOfRangeException();
			}

			renderPass.Exec?.Invoke(graphicsCommandBuffer); // see Exec comment
		}
		*/
	}

	public readonly record struct RenderPassHandle { // TODO IEquatable
		public required string Name { get; init; }

		[SetsRequiredMembers]
		public RenderPassHandle(string name) => Name = name;
	}

	public interface IResourceHandle { // TODO IEquatable
		public string Name { get; init; }
	}

	// ref?
	public readonly record struct BufferHandle : IResourceHandle {
		public required string Name { get; init; }

		[SetsRequiredMembers]
		public BufferHandle(string name) => Name = name;
	}

	// ref?
	public readonly record struct ImageHandle : IResourceHandle {
		public required string Name { get; init; }

		[SetsRequiredMembers]
		public ImageHandle(string name) => Name = name;
	}

	// TODO i could use the new unions for these?
	private abstract class ResourceData { // TODO rename
		internal List<RenderPassHandle> Reads { get; } = new();
		internal List<RenderPassHandle> Writes { get; } = new();
	}

	private class BufferResourceData : ResourceData { // TODO
		public VulkanBuffer Buffer { get; }

		public BufferResourceData(VulkanResourceManager resourceManager, string resourceName, ulong size, VkBufferUsageFlagBits2 usageFlags, VkBufferCreateFlagBits createFlags,
			VkMemoryPropertyFlagBits memoryPropertyFlags) =>
				Buffer = resourceManager.CreateBuffer(resourceName, size, usageFlags, createFlags, memoryPropertyFlags);
	}

	private class ImageResourceData : ResourceData { // TODO
		public VkFormat Format { get; }
		public VkExtent2D Extent { get; }
		public VkImageUsageFlagBits Usage { get; }
		public VkImageLayout InitialLayout { get; }
		public VkImageLayout FinalLayout { get; }

		public VulkanTexture Texture { get; }

		public ImageResourceData(VulkanResourceManager resourceManager, string resourceName, VkFormat format, VkExtent2D extent, VkImageUsageFlagBits usage, VkImageLayout initialLayout, VkImageLayout finalLayout) {
			Format = format;
			Extent = extent;
			Usage = usage;
			InitialLayout = initialLayout;
			FinalLayout = finalLayout;
			Texture = resourceManager.CreateTexture(resourceName);
		}
	}
}