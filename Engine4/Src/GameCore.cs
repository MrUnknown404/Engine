using Engine4.Utility.Versions;

namespace Engine4;

public abstract class GameCore {
	public string Name { get; }
	public IPackableVersion Version { get; }
	// TODO more properties

	public ushort TargetFps { get; init; }
	public ushort TargetUps { get; init; }
	public byte MaxFrameSkip { get; init; } = 5;

	public ulong UpdateCount { get; private set; }
	public ulong FrameCount { get; private set; }

	// lifecycle
	public bool IsRunning { get; private set; }
	private bool shouldShutdown;

	protected abstract Action? PollEvents { get; }

	public event Action? OnSetupStartEvent;
	public event Action? OnSetupDoneEvent;
	public event Action? OnExitEvent;

	public event RequestShutdownDelegate? RequestShutdownEvent;

	protected GameCore(string name, IPackableVersion version) {
		Name = name;
		Version = version;
	}

	public void Start(string[] args) {
		// setup
		InitialSetup();
		Console.WriteLine("setup");

		ProcessArgs(args);

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

	protected virtual void ProcessArgs(string[] args) {
		// TODO
	}

	protected virtual void SetupInternals() {
		// TODO
	}

	protected abstract void Setup();
	protected abstract void Update();
	protected abstract void Render(float delta);

	private void GameLoop() {
		// TODO

		Console.WriteLine("in loop");

		IsRunning = true;
		while (IsRunning) {
			PollEvents?.Invoke();

			if (shouldShutdown) { break; }

			InternalUpdate();
			Update();

			float delta = 0; // TODO
			Render(delta);

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

		OnExitEvent?.Invoke();

		InternalCleanup();
		Cleanup();
	}

	protected virtual void InternalCleanup() {
		// TODO
	}

	protected abstract void Cleanup();

	public delegate bool RequestShutdownDelegate(ref bool shouldShutdown);
}