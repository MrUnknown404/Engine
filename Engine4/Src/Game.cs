namespace Engine4;

public abstract class Game {
	public Engine Engine;

	public bool ShouldShutdown { get; set; }

	/// <summary> Called right at the start before anything is set up </summary>
	public event Action? OnStartEvent;
	public event Action? OnSetupEvent;

	protected Game(Engine engine) => Engine = engine;

	protected internal abstract void Update();

	internal void InvokeOnStartEvent() => OnStartEvent?.Invoke();
	internal void InvokeOnSetupEvent() => OnSetupEvent?.Invoke();
}