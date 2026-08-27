namespace Engine4;

public abstract class Game {
	public Engine Engine { get; }

	public bool ShouldShutdown { get; set; }

	/// <summary> Called right at the start before anything is set up </summary>
	public event Action? OnStartEvent;
	public event Action? OnSetupEvent;
	public event Action? OnSetupDoneEvent;

	public event ShouldShutdownDelegate? ShouldShutdownEvent;

	protected Game(Engine engine) => Engine = engine;

	protected internal abstract void Update();

	public void RequestShutdown() {
		bool shouldShutdown = true;
		ShouldShutdownEvent?.Invoke(ref shouldShutdown);
		if (shouldShutdown) { ShouldShutdown = true; }
	}

	internal void InvokeOnStartEvent() => OnStartEvent?.Invoke();
	internal void InvokeOnSetupEvent() => OnSetupEvent?.Invoke();
	internal void InvokeOnSetupDoneEvent() => OnSetupDoneEvent?.Invoke();

	public delegate void ShouldShutdownDelegate(ref bool shouldShutdown);
}