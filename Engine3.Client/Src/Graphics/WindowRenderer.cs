using System.Diagnostics.CodeAnalysis;
using Engine3.Client.Graphics.ImGui;
using Engine3.Client.Graphics.OpenGL.Renderers;
using Engine3.Client.Graphics.Vulkan.Renderers;
using Engine3.Client.IO;
using Engine3.Core.Graphics;
using Engine3.Core.Utility;
using Engine3.Core.Utility.Exceptions;
using ImGuiNET;

namespace Engine3.Client.Graphics;

public abstract class WindowRenderer : Renderer {
	public abstract Window Window { get; }
	public abstract IGraphicsResourceProvider GraphicsResourceProvider { get; }

	public _3DGraphicsApi _3DGraphicsApi { get; }

	public ImGuiBackend? ImGuiBackend { get; init; }
	public ImGuiRenderer? ImGuiRenderer { get; init; }
	public bool UseImGui { get; init; }

	protected WindowRenderer(_3DGraphicsApi graphicsApi) : base(graphicsApi switch {
			_3DGraphicsApi.OpenGL => GraphicsApi.OpenGL,
			_3DGraphicsApi.Vulkan => GraphicsApi.Vulkan,
			_ => throw new ArgumentOutOfRangeException(nameof(graphicsApi), graphicsApi, null),
	}) =>
			_3DGraphicsApi = graphicsApi;

	protected sealed override void Render(float delta) {
		PrepareRender(); // opengl needs context bound (if multi windowed). i could just put it in TryCleanup but i'd rather it be separate
		TryCleanupResources(); // TODO don't destroy every frame?

		if (!TryNextFrame()) { return; }

		// copy
		bool hasImGuiDrawData = TryImGuiNewFrame(out ImDrawDataPtr? imDrawData); // backend/renderer won't be null if true

		CopyBuffers(delta);
		if (hasImGuiDrawData) { ImGuiRenderer!.CopyBuffers(imDrawData!.Value); }

		// draw
		BeginFrame();

		DrawFrame();
		if (hasImGuiDrawData) { ImGuiRenderer!.DrawFrame(imDrawData!.Value); }

		EndFrame();

		return;

		bool TryImGuiNewFrame([NotNullWhen(true)] out ImDrawDataPtr? imDrawData) {
			if (UseImGui && ImGuiBackend!.NewFrame(out ImDrawDataPtr drawData)) {
				imDrawData = drawData;
				SetImGuiFrameData();
				return true;
			}

			imDrawData = null;
			return false;
		}
	}

	protected void CreateImGui(out ImGuiBackend imGuiBackend, out ImGuiRenderer imGuiRenderer, Action? showImGui = null) {
		imGuiBackend = new(Window, _3DGraphicsApi, showImGui);
		imGuiRenderer = _3DGraphicsApi switch {
				_3DGraphicsApi.OpenGL => new OpenGLImGuiRenderer(imGuiBackend, this as OpenGLRendererBase ?? throw new Engine3Exception($"Renderer must be of type {nameof(OpenGLRendererBase)}")),
				_3DGraphicsApi.Vulkan => new VulkanImGuiRenderer(imGuiBackend, this as VulkanRendererBase ?? throw new Engine3Exception($"Renderer must be of type {nameof(VulkanRendererBase)}")),
				_ => throw new ArgumentOutOfRangeException(nameof(GraphicsApi), GraphicsApi, null),
		};
	}

	protected abstract void SetImGuiFrameData();
}