using Silk.NET.GLFW;

namespace Engine4.Client.IO;

public class GlfwHandler { // TODO merge into gameclient?
	public Glfw Glfw { get; }

	internal GlfwHandler() {
		try { Glfw = Glfw.GetApi(); }
		catch (FileNotFoundException e) { throw new Exception("Glfw not found. Do you have Glfw installed?", e); } // TODO exception

		Glfw.SetErrorCallback(ErrorCallback); // TODO untested
		Glfw.Init();
	}

	private static void ErrorCallback(ErrorCode error, string description) => Console.WriteLine($"[GLFW] [{error}] {description}"); // TODO log

	internal void Cleanup() {
		Glfw.Terminate();
		Glfw.SetErrorCallback(null);
	}
}