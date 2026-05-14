using System.Diagnostics.CodeAnalysis;
using Engine3.Client.Graphics.ImGui;
using Engine3.Client.Graphics.OpenGL.Renderers;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Renderers;
using Engine3.Exceptions;
using ImGuiNET;
using NLog;

namespace Engine3.Client.Graphics;

public abstract class Renderer {
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public _3DGraphicsApi GraphicsApi { get; }

	public bool CanRender { get; set; } = true;
	public bool ShouldDestroy { get; protected set; }
	public abstract bool IsHidden { get; }

	public bool WasDestroyed { get; private set; }

	internal Renderer(_3DGraphicsApi graphicsApi) => GraphicsApi = graphicsApi;

	protected internal virtual void Update() { }
	protected internal abstract void Render(float delta);

	public abstract bool IsSameWindow(Window window);

	internal void Destroy() {
		if (WasDestroyed) {
			Logger.Warn($"{GetType().Name} was already destroyed");
			return;
		}

		PrepareCleanup();
		CleanupImGui();
		Cleanup();

		WasDestroyed = true;
	}

	protected abstract void PrepareCleanup();
	protected abstract void Cleanup();

	[SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
	internal abstract void CleanupImGui();
}

public abstract class Renderer<TWindow, TResourceProvider> : Renderer where TWindow : Window where TResourceProvider : IGraphicsResourceProvider {
	public TResourceProvider GraphicsResourceProvider { get; }

	protected TWindow Window { get; }

	protected ImGuiBackend? ImGuiBackend { get; init; }
	protected ImGuiRenderer? ImGuiRenderer { get; init; }
	protected bool UseImGui { get; init; }

	public override bool IsHidden => Window.IsHidden;

	protected Renderer(TWindow window, _3DGraphicsApi graphicsApi) : base(graphicsApi) {
		Window = window;
		GraphicsResourceProvider = (TResourceProvider)window.GraphicsResourceProvider;
	}

	protected void CreateImGui(out ImGuiBackend imGuiBackend, out ImGuiRenderer imGuiRenderer) {
		imGuiBackend = new(Window, GraphicsApi);
		imGuiRenderer = GraphicsApi switch {
				_3DGraphicsApi.OpenGL => new OpenGLImGuiRenderer(imGuiBackend, this as OpenGLRendererBase ?? throw new Engine3Exception($"Renderer must be of type {nameof(OpenGLRendererBase)}")),
				_3DGraphicsApi.Vulkan => new VulkanImGuiRenderer(imGuiBackend, this as VulkanRendererBase ?? throw new Engine3Exception($"Renderer must be of type {nameof(VulkanRendererBase)}")),
				_ => throw new ArgumentOutOfRangeException(nameof(GraphicsApi), GraphicsApi, null),
		};
	}

	protected internal override void Render(float delta) {
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

	protected abstract void PrepareRender();
	protected abstract void TryCleanupResources();
	protected abstract bool TryNextFrame();
	protected abstract void CopyBuffers(float delta);
	protected abstract void BeginFrame();
	protected abstract void DrawFrame();
	protected abstract void EndFrame();

	protected abstract void SetImGuiFrameData();

	public override bool IsSameWindow(Window window) => Window == window;

	internal override void CleanupImGui() => ImGuiBackend?.Cleanup();
}