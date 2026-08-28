using Engine4.IO;
using OpenTK.Platform;

namespace Engine4.Graphics.IO;

public class OpenTKEventHandler : IEventHandler {
	public void ProcessEvents() => Toolkit.Window.ProcessEvents(false);
}