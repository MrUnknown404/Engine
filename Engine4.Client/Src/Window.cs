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

	public event RequestCloseDelegate? RequestCloseEvent;

	internal Window(string title, ushort width, ushort height, Action? setWindowHints = null) {
		GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.NoApi); // disable opengl
		GLFW.WindowHint(WindowHintBool.Decorated, true);
		setWindowHints?.Invoke(); // user window hints

		glfwWindow = GLFW.CreateWindow(width, height, title, null, null);
		GLFW.DefaultWindowHints(); // reset hints
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

	public delegate bool RequestCloseDelegate(ref bool shouldShutdown);
}