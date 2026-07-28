using Engine4.Graphics;
using JetBrains.Annotations;

namespace Engine4;

// TODO logging

[MustDisposeResource]
public sealed class Engine : IDisposable {
	private static Engine? engineInstance;
	public static Engine EngineInstance { get => engineInstance ?? throw new NullReferenceException(); private set => engineInstance = value; } // TODO exception
	public static Game? GameInstance { get; private set; }

	public GraphicsApi GraphicsApi { get; }

	public ushort TargetFps { get; init; }
	public ushort TargetUps { get; init; }

	public bool ShouldShutdown { get; set; }
	public bool IsRunning { get; private set; }

	private bool wasDisposed;

	public Engine(string[] args, GraphicsApi graphicsApi) { // TODO process args
		if (engineInstance != null) { throw new NullReferenceException(); }
		EngineInstance = this;

		GraphicsApi = graphicsApi;
	}

	public void Start<T>(T game) where T : Game {
		if (GameInstance != null) { throw new NullReferenceException(); } // TODO exception
		GameInstance = game;

		game.InvokeOnStartEvent();

		Setup();
		game.InvokeOnSetupEvent();
		SetupPostGame();

		GameLoop(game);

		Cleanup();
	}

	private void Setup() { }
	private void SetupPostGame() { }

	private void GameLoop(Game game) {
		Action tryProcessEvents = GraphicsApi.TryProcessEvents;

		IsRunning = true;
		while (IsRunning) {
			tryProcessEvents();

			if (TryExitEarly(game)) {
				IsRunning = false;
				break;
			}

			float delta = 0;

			Update();
			game.Update();

			Render(delta);

			Thread.Sleep(1); // TODO remove
		}

		return;

		bool TryExitEarly(Game game) => ShouldShutdown || game.ShouldShutdown;
	}

	private void Update() { }

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