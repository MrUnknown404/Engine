using System.Diagnostics.CodeAnalysis;
using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Graphics.Vulkan.Objects;
using Engine4.Utility.Exceptions;
using JetBrains.Annotations;

namespace Engine4.Client.Graphics._Test;

// TODO look into
//  https://en.wikipedia.org/wiki/Directed_acyclic_graph
//  https://en.wikipedia.org/wiki/Topological_sorting#Kahn's_algorithm
//  https://www.gdcvault.com/play/1024612/FrameGraph-Extensible-Rendering-Architecture-in
//  https://themaister.net/blog/2017/08/15/render-graphs-and-vulkan-a-deep-dive/
//  https://apoorvaj.io/render-graphs-1
//  https://vkguide.dev/docs/ascendant/ascendant_light/
//  https://dev.to/p3ngu1nzz/advanced-vulkan-rendering-building-a-modern-frame-graph-and-memory-management-system-15kn
//  https://www.geeksforgeeks.org/dsa/introduction-to-directed-acyclic-graph/
//  https://www.geeksforgeeks.org/dsa/topological-sorting-indegree-based-solution/
// TODO document this when done

public class RenderGraph {
	private bool isDirty;

	private readonly Dictionary<RenderPassHandle, RenderPass2> renderPasses = new();
	private RenderPass2[] bakedRenderPasses = Array.Empty<RenderPass2>();

	private readonly Dictionary<string, ResourceData> resources = new(); // TODO how efficient is using a string as a hash code? should i make my own type? it's probably fine?

	private readonly VulkanResourceManager resourceManager;

	private static void Test(VulkanResourceManager resourceManager) { // test example
		RenderGraph renderGraph = new(resourceManager);

		const ulong Size = 0; // make vulkan resources here
		BufferHandle vertexBuffer = renderGraph.AddBuffer("vertex buffer", Size); // TODO what if we want to resize later? auto resize behind the scenes?
		BufferHandle indexBuffer = renderGraph.AddBuffer("index buffer", Size);
		// ImageHandle testImage = renderGraph.AddTexture("test image");

		RenderPassBuilder passBuilder = new("testPass", RenderPassStage.Graphics, null); // compute/graphics/transfer
		passBuilder.AddInput(vertexBuffer);
		passBuilder.AddInput(indexBuffer);
		// passBuilder.AddInput(testImage);
		RenderPassHandle testPass = renderGraph.AddPass(passBuilder);

		renderGraph.DisablePass(testPass);
		renderGraph.EnablePass(testPass);

		renderGraph.RemovePass(testPass);
	}

	internal RenderGraph(VulkanResourceManager resourceManager) => this.resourceManager = resourceManager;

	[MustUseReturnValue]
	public BufferHandle AddBuffer(string resourceName, ulong size) {
		if (!resources.TryGetValue(resourceName, out ResourceData? resourceData)) {
			resourceData = new(resourceManager.CreateBuffer(resourceName, size));
			resources.Add(resourceName, resourceData);
			return new(resourceName);
		} else { throw new Exception(); } // TODO exception
	}

	[MustUseReturnValue]
	public ImageHandle AddTexture(string resourceName) { // TODO properties
		if (!resources.TryGetValue(resourceName, out ResourceData? resourceData)) {
			resourceData = new(resourceManager.CreateTexture(resourceName));
			resources.Add(resourceName, resourceData);
			return new(resourceName);
		} else { throw new Exception(); } // TODO exception
	}

	[MustUseReturnValue]
	public BufferHandle RemoveBuffer(string resourceName) => throw new NotImplementedException(); // TODO

	[MustUseReturnValue]
	public ImageHandle RemoveTexture(string resourceName) => throw new NotImplementedException(); // TODO

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

		// TODO cleanup resources if no longer used
	}

	public void DisablePass(RenderPassHandle handle) => renderPasses[handle].Enabled = false;
	public void EnablePass(RenderPassHandle handle) => renderPasses[handle].Enabled = true;

	private RenderPass2[] BakeGraph() {
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

		// TODO make this output
		/* Example Output:
		 * [
		 * - [ Pass1, [ Pass2, Pass3 ], ],
		 * - [ Pass2, [ Pass3, ], ],
		 * - [ Pass3, [ ], ],
		 * ]
		 */
		[MustUseReturnValue]
		static Dictionary<RenderPass2, RenderPass2[]> CalculateEdges(Dictionary<RenderPassHandle, RenderPass2> renderPasses, Dictionary<string, ResourceData> resources) {
			Dictionary<RenderPass2, RenderPass2[]> edges = new();

			// calculate first/last used here?
			// USE renderPass.Enabled

			foreach (KeyValuePair<RenderPassHandle, RenderPass2> renderPass in renderPasses) { }
			foreach (KeyValuePair<string, ResourceData> resourceData in resources) { }

			return edges;
		}

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
	}

	private void Render(GraphicsCommandBuffer graphicsCommandBuffer) {
		if (isDirty) {
			bakedRenderPasses = BakeGraph();
			isDirty = false;
		}

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

	private class ResourceData { // TODO rename
		internal VulkanResource Resource { get; }

		internal List<RenderPassHandle> Reads { get; } = new();
		internal List<RenderPassHandle> Writes { get; } = new();

		public ResourceData(VulkanResource resource) => this.Resource = resource;
	}
}