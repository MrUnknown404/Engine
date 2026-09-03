using JetBrains.Annotations;

namespace Engine4;

// TODO logging

[Obsolete]
[MustDisposeResource]
public sealed class Engine : IDisposable {
	private static Engine? engineInstance;

	public static Engine EngineInstance { get => engineInstance ?? throw new NullReferenceException($"{nameof(EngineInstance)} does not exist yet"); private set => engineInstance = value; }
	public static Game? GameInstance { get; private set; }

	public ushort TargetFps { get; init; }
	public ushort TargetUps { get; init; }

	public bool ShouldShutdown { get; set; }
	public bool IsRunning { get; private set; }

	private bool wasDisposed;

	public Engine(string[] args) {
		if (engineInstance != null) { throw new NullReferenceException(); }
		EngineInstance = this;

		ProcessArgs(args);
	}

	private void ProcessArgs(string[] args) { } // TODO process args

	public void Start<T>(T game) where T : Game {
		if (GameInstance != null) { throw new NullReferenceException(); } // TODO log and return
		GameInstance = game;

		game.InvokeOnStartEvent();

		// setup
		Setup();
		game.InternalSetup();
		game.InvokeOnSetupEvent();
		LateSetup();

		// done
		game.InvokeOnSetupDoneEvent();
		GameLoop(game);

		Cleanup();
	}

	private void Setup() { }
	private void LateSetup() { }

	private void GameLoop(Game game) {
		IsRunning = true;

		while (IsRunning) {
			game.EventHandler?.ProcessEvents();

			if (ShouldShutdown || game.ShouldShutdown) { break; } // try early exit

			float delta = 0;

			Update(game);
			game.Update();

			Render(delta);

			Thread.Sleep(1); // TODO remove
		}

		IsRunning = false;
	}

	private void Update(Game game) { game.TryFreeResources(); }

	private void Render(float delta) {
		// copy data
		// draw
	}

	public void Dispose() {
		if (wasDisposed) { return; }

		Cleanup();

		wasDisposed = true;
	}

	private void Cleanup() {
		// TODO cleanup engine

		GameInstance?.Cleanup();
	}
}