using Engine4.IO;
using OpenTK.Platform;

namespace Engine4.Client.IO;

public class OpenTKEventHandler : IEventHandler {
	public void ProcessEvents() => Toolkit.Window.ProcessEvents(false);

	internal static void OnOpenTkEvent(EventArgs args) {
		switch (args) { // TODO add more
			case CloseEventArgs closeArgs:
				if (closeArgs.Window.UserData is not Window window) { throw new Exception(); } // TODO exception

				window.RequestClose();
				break;
			default: break;
		}
	}
}