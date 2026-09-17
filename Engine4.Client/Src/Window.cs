using JetBrains.Annotations;
using OpenTK.Windowing.GraphicsLibraryFramework;
using USharpLibs.Common.Math;
using GlfwWindow = OpenTK.Windowing.GraphicsLibraryFramework.Window;

namespace Engine4.Client;

public unsafe class Window {
	internal GlfwWindow* GlfwWindow { get; } // TODO private

	public bool ShouldClose { get; private set; }

	public event RequestCloseDelegate? RequestCloseEvent;

	internal Window(string title, ushort width, ushort height, Action? setWindowHints = null) {
		GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.NoApi); // disable opengl
		GLFW.WindowHint(WindowHintBool.Decorated, true);
		setWindowHints?.Invoke(); // user window hints

		GlfwWindow = GLFW.CreateWindow(width, height, title, null, null);
		GLFW.DefaultWindowHints(); // reset hints
	}

	public void Show() => GLFW.ShowWindow(GlfwWindow);
	public void Hide() => GLFW.HideWindow(GlfwWindow);

	/// <summary> Not cached </summary>
	[MustUseReturnValue]
	public Vec2<ushort> GetFrameBufferSize() {
		GLFW.GetFramebufferSize(GlfwWindow, out int width, out int height);
		checked { return new((ushort)width, (ushort)height); } // throw if we lose data. this should never happen
	}

	public void RequestClose(bool force) {
		if (force) {
			ShouldClose = true;
			return;
		}

		bool shouldClose = true;
		RequestCloseEvent?.Invoke(ref shouldClose);
		if (shouldClose) { ShouldClose = true; }
	}

	internal bool GlfwShouldClose() => GLFW.WindowShouldClose(GlfwWindow);

	internal void Cleanup() => GLFW.DestroyWindow(GlfwWindow); // TODO why isn't this working?

	public delegate bool RequestCloseDelegate(ref bool shouldShutdown);
}