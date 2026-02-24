using System.Diagnostics.CodeAnalysis;
using Engine3.Client.Graphics.ImGui;
using ImGuiNET;
using NLog;

namespace Engine3.Client.Graphics {
	public abstract class Renderer {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public bool CanRender { get; set; } = true;
		public bool ShouldDestroy { get; protected set; }
		public abstract bool IsHidden { get; }

		public bool WasDestroyed { get; private set; }

		public event Action? OnSetupDoneEvent;

		internal Renderer() { }

		protected internal abstract void Setup();
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

		internal abstract void CleanupImGui();

		internal void InvokeOnSetupDoneEvent() => OnSetupDoneEvent?.Invoke();
	}

	public abstract class Renderer<TWindow, TBackend, TImGui> : Renderer where TWindow : Window where TBackend : EngineGraphicsBackend where TImGui : ImGuiBackend {
		protected TBackend GraphicsBackend { get; }
		protected TWindow Window { get; }
		protected TImGui? ImGuiBackend { get; init; }

		public override bool IsHidden => Window.IsHidden;

		protected Renderer(TBackend graphicsBackend, TWindow window) {
			GraphicsBackend = graphicsBackend;
			Window = window;
		}

		protected internal override void Render(float delta) {
			PrepareRender();
			TryCleanupResources(); // TODO don't destroy every frame?

			if (!TryNextFrame()) { return; }

			bool shouldImGui = TryImGuiNewFrame(out ImDrawDataPtr? imDrawData); // backend won't be null if true

			CopyBuffers(delta);
			if (shouldImGui) { ImGuiBackend!.UpdateBuffers(imDrawData!.Value); } // annotation doesn't work

			BeginFrame();

			DrawFrame();
			if (shouldImGui) { DrawImGuiFrame(imDrawData!.Value); } // annotation doesn't work

			EndFrame();

			return;

			bool TryImGuiNewFrame([NotNullWhen(true)] out ImDrawDataPtr? imDrawData) {
				if (ImGuiBackend != null && ImGuiBackend.NewFrame(out ImDrawDataPtr drawData)) {
					imDrawData = drawData;
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
		protected abstract void DrawImGuiFrame(ImDrawDataPtr imDrawData);
		protected abstract void EndFrame();

		public override bool IsSameWindow(Window window) => Window == window;

		internal override void CleanupImGui() => ImGuiBackend?.Cleanup();
	}
}