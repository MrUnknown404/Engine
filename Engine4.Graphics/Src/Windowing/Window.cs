namespace Engine4.Graphics.Windowing;

public class Window {
	// TODO input is window specific
	// TODO should windows be comparable?
	// TODO opentk only?

	public bool ShouldClose { get; set; } // TODO use

	public event TryCloseWindowDelegate? TryCloseWindowEvent;

	public event Action? OnWindowClosedEvent; // TODO call

	public void Show() => throw new NotImplementedException(); // TODO

	public void RequestClose() {
		bool shouldClose = true;
		TryCloseWindowEvent?.Invoke(ref shouldClose);
		if (shouldClose) { ShouldClose = true; }
	}

	public delegate void TryCloseWindowDelegate(ref bool shouldClose);
}