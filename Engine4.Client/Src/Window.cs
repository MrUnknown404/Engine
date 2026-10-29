using Engine4.Client.Graphics.Vulkan.Objects;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;
using OpenTK.Windowing.GraphicsLibraryFramework;
using USharpLibs.Common.Math;
using GlfwWindow = OpenTK.Windowing.GraphicsLibraryFramework.Window;

namespace Engine4.Client;

public unsafe class Window {
	private readonly GlfwWindow* glfwWindow;

	public bool ShouldClose { get; private set; }
	public bool IsFrameBufferSizeDirty { get; internal set; }
	public Vec2<double> CursorPosition { get; private set; }

	// i need to store these here otherwise i get a crash
	private readonly GLFWCallbacks.KeyCallback keyCallback;
	private readonly GLFWCallbacks.MouseButtonCallback mouseButtonCallback;
	private readonly GLFWCallbacks.CursorPosCallback cursorPosCallback;
	private readonly GLFWCallbacks.ScrollCallback scrollCallback;
	private readonly GLFWCallbacks.FramebufferSizeCallback framebufferSizeCallback;

	public event OnKeyDelegate? OnKeyEvent;
	public event OnMouseButtonDelegate? OnMouseButtonEvent;
	public event OnScrollDelegate? OnScrollEvent;

	public event RequestCloseDelegate? RequestCloseEvent;

	internal Window(string title, ushort width, ushort height, Action? setWindowHints) {
		GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.NoApi); // disable opengl
		GLFW.WindowHint(WindowHintBool.Decorated, true);
		setWindowHints?.Invoke(); // user window hints

		glfwWindow = GLFW.CreateWindow(width, height, title, null, null);
		GLFW.DefaultWindowHints(); // reset hints

		// callbacks
		GLFW.SetKeyCallback(glfwWindow, keyCallback = GlfwKeyCallback);
		GLFW.SetMouseButtonCallback(glfwWindow, mouseButtonCallback = GlfwMouseButtonCallback);
		GLFW.SetScrollCallback(glfwWindow, scrollCallback = GlfwScrollCallback);
		GLFW.SetCursorPosCallback(glfwWindow, cursorPosCallback = GlfwCursorPosCallback);
		GLFW.SetFramebufferSizeCallback(glfwWindow, framebufferSizeCallback = GlfwFramebufferSizeCallback);
	}

	// for some reason 'window' is always the last window registered? not the window receiving hte action?
	private void GlfwKeyCallback(GlfwWindow* window, Keys key, int scanCode, InputAction action, KeyModifiers mods) {
		// if (useImGui) {
		// 	// TODO pass to imgui. if failed, send to client
		// }

		OnKeyEvent?.Invoke(key, scanCode, action, mods);
	}

	private void GlfwMouseButtonCallback(GlfwWindow* window, MouseButton button, InputAction action, KeyModifiers mods) {
		// if (useImGui) {
		// 	// TODO pass to imgui. if failed, send to client
		// }

		OnMouseButtonEvent?.Invoke(button, action, mods);
	}

	private void GlfwScrollCallback(GlfwWindow* window, double offsetX, double offsetY) {
		// if (useImGui) {
		// 	// TODO pass to imgui. if failed, send to client
		// }

		OnScrollEvent?.Invoke(offsetX, offsetY);
	}

	private void GlfwCursorPosCallback(GlfwWindow* window, double x, double y) => CursorPosition = new(x, y);

	private void GlfwFramebufferSizeCallback(GlfwWindow* window, int width, int height) {
		// if (useImGui) {
		// 	// TODO pass to imgui
		// }

		IsFrameBufferSizeDirty = true;
	}

	public void Show() => GLFW.ShowWindow(glfwWindow);
	public void Hide() => GLFW.HideWindow(glfwWindow);

	/// <summary> Not cached </summary>
	[MustUseReturnValue]
	public Vec2<ushort> GetFrameBufferSize() {
		GLFW.GetFramebufferSize(glfwWindow, out int width, out int height);
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

	internal bool GlfwShouldClose() => GLFW.WindowShouldClose(glfwWindow);

	internal VkSurfaceKHR CreateSurface(VulkanInstance vulkanInstance) {
		GLFW.CreateWindowSurface(new((ulong)vulkanInstance.VkInstance.Handle), glfwWindow, null, out VkHandle handle);
		return new(handle.Handle);
	}

	internal void Cleanup() => GLFW.DestroyWindow(glfwWindow);

	public delegate void OnKeyDelegate(Keys key, int scanCode, InputAction action, KeyModifiers mods);
	public delegate void OnMouseButtonDelegate(MouseButton button, InputAction action, KeyModifiers mods);
	public delegate void OnScrollDelegate(double offsetX, double offsetY);

	public delegate bool RequestCloseDelegate(ref bool shouldShutdown);
}