using Engine4.Graphics;
using Engine4.IO;
using JetBrains.Annotations;

namespace Engine4;

// TODO logging

[MustDisposeResource]
public sealed class Engine : IDisposable {
	private static Engine? engineInstance;

	public static Engine EngineInstance { get => engineInstance ?? throw new NullReferenceException(); private set => engineInstance = value; } // TODO exception
	public static Game? GameInstance { get; private set; }

	public GraphicsApi GraphicsApi { get; }
	public IEventHandler? EventHandler { get; }

	public ushort TargetFps { get; init; }
	public ushort TargetUps { get; init; }

	public bool ShouldShutdown { get; set; }
	public bool IsRunning { get; private set; }

	private bool wasDisposed;

	public Engine(string[] args, GraphicsApi graphicsApi, IEventHandler eventHandler) { // TODO process args
		if (engineInstance != null) { throw new NullReferenceException(); }
		EngineInstance = this;

		GraphicsApi = graphicsApi;
		EventHandler = eventHandler;
	}

	public void Start<T>(T game) where T : Game {
		if (GameInstance != null) { throw new NullReferenceException(); } // TODO exception
		GameInstance = game;

		// TODO init opentk if used. figure that out

		game.InvokeOnStartEvent();

		// setup
		Setup();
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
			EventHandler?.ProcessEvents();

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

	private void Cleanup() { } // TODO
}