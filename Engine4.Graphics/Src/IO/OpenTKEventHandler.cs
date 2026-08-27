using Engine4.Graphics.Windowing;

// ReSharper disable once CheckNamespace
namespace Engine4.IO; // TODO ^^ handle that

public class OpenTKEventHandler : IEventHandler {
	public void ProcessEvents() {
		//
	}

	public void RegisterWindow(Window window) => throw new NotImplementedException(); // TODO store
}