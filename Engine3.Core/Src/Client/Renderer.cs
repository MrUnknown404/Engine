using Engine3.Core.Utility;
using NLog;

namespace Engine3.Core.Client;

public abstract class Renderer {
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public GraphicsApi GraphicsApi { get; }
	public bool ShouldRender { get; set; } = true;
	public bool ShouldDestroy { get; protected set; }

	public bool WasDestroyed { get; private set; }

	protected Renderer(GraphicsApi graphicsApi) => GraphicsApi = graphicsApi;

	protected internal virtual void Update() { }

	protected internal virtual void Render(float delta) {
		PrepareRender(); // opengl needs context bound (if multi windowed). i could just put it in TryCleanup but i'd rather it be separate
		TryCleanupResources(); // TODO don't destroy every frame?

		if (!TryNextFrame()) { return; }

		// copy
		CopyBuffers(delta);

		// draw
		BeginFrame();
		DrawFrame();
		EndFrame();
	}

	protected abstract void PrepareRender();
	protected abstract void TryCleanupResources();
	protected abstract bool TryNextFrame();
	protected abstract void CopyBuffers(float delta);
	protected abstract void BeginFrame();
	protected abstract void DrawFrame();
	protected abstract void EndFrame();

	internal void Destroy() {
		if (WasDestroyed) {
			Logger.Warn($"{GetType().Name} was already destroyed");
			return;
		}

		PrepareCleanup();
		Cleanup();

		WasDestroyed = true;
	}

	protected abstract void PrepareCleanup();
	protected abstract void Cleanup();
}