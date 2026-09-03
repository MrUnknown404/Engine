namespace Engine4;

public abstract class Game2 {
	public string Name { get; }
	// TODO more properties

	// lifecycle
	public bool IsRunning { get; private set; }
	private bool shouldShutdown;

	protected abstract Action? PollEvents { get; }

	public event Action? OnSetupStartEvent;
	public event Action? OnSetupDoneEvent;

	public event RequestShutdownDelegate? RequestShutdownEvent;

	protected Game2(string[] args, string name) => Name = name;

	public void Start() {
		// setup
		InitialSetup();
		Console.WriteLine("setup");

		OnSetupStartEvent?.Invoke();
		SetupInternals();

		Setup();

		OnSetupDoneEvent?.Invoke();
		Console.WriteLine("done");

		// loop
		GameLoop();

		// exit
		OnExit();
		Console.WriteLine("exit");
	}

	private void InitialSetup() {
		// TODO logging
		// TODO thread name
	}

	protected virtual void SetupInternals() {
		// TODO
	}

	protected abstract void Setup();
	protected abstract void Update();

	private void GameLoop() {
		// TODO

		Console.WriteLine("in loop");

		IsRunning = true;
		while (IsRunning) {
			PollEvents?.Invoke();

			if (shouldShutdown) { break; }

			InternalUpdate();
			Update();

			float delta; // TODO

			// if (CanRender) { // begin render, copy data, render
			// 	CopyData(delta);
			// 	Render();
			// }

			Thread.Sleep(1); // TODO remove
		}

		IsRunning = false;
	}

	protected virtual void InternalUpdate() {
		// TODO
	}

	public void RequestShutdown(bool force) {
		if (force) {
			this.shouldShutdown = true;
			return;
		}

		bool shouldShutdown = true;
		RequestShutdownEvent?.Invoke(ref shouldShutdown);
		if (shouldShutdown) { this.shouldShutdown = true; }
	}

	private void OnExit() {
		// TODO

		InternalCleanup();
		Cleanup();
	}

	protected virtual void InternalCleanup() {
		// TODO
	}

	protected abstract void Cleanup();

	public delegate bool RequestShutdownDelegate(ref bool shouldShutdown);
}