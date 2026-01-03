using System.Diagnostics;
using Engine3.Exceptions;
using Engine3.Graphics.Vulkan;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;
using OpenTK.Platform;
using GraphicsApi = Engine3.Graphics.GraphicsApi;

namespace Engine3.Utils {
	public class EngineWindow {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public WindowHandle WindowHandle { get; }

		public VkSurfaceKHR? VkSurface { get; private set; }
		public Gpu[] Gpus { get; private set; } = Array.Empty<Gpu>(); // TODO i'd like to not store this here but VkH#CreateGpu needs a surface. figure out how to store this
		public Gpu? BestGpu { get; private set; }
		public VkDevice? VkLogicalDevice { get; private set; }
		public VkQueue? VkGraphicsQueue { get; private set; }
		public VkQueue? VkPresentQueue { get; private set; }

		public event AttemptCloseWindow? TryCloseWindowEvent;
		public event Action? OnCloseWindowEvent;

		private bool wasCleanedUp;

		private EngineWindow(WindowHandle windowHandle) => WindowHandle = windowHandle;

		[MustUseReturnValue]
		public static EngineWindow CreateWindow(string title, int w, int h) {
			if (Engine3.GraphicsApi == GraphicsApi.Console) { throw new Engine3Exception("Cannot create window when using Console graphics api"); }
			if (!Engine3.WasGraphicsApiSetup) { throw new Engine3Exception("Cannot create a window with no graphics api setup"); }

			WindowHandle windowHandle = Toolkit.Window.Create(Engine3.GraphicsApiHints!); // Engine3.GraphicsApiHints shouldn't be null here
			Logger.Info("Created new window");

			Toolkit.Window.SetTitle(windowHandle, title);
			Toolkit.Window.SetSize(windowHandle, new(w, h));

			// 	if (settings.CenterWindow) {
			// 		DisplayHandle handle = Toolkit.Display.OpenPrimary(); // TODO figure out if this is what i am supposed to do
			// 		Toolkit.Display.GetResolution(handle, out int width, out int height);
			// 		Toolkit.Window.SetPosition(windowHandle, new(width / 2 - DefaultWidth / 2, height / 2 - DefaultHeight / 2)); // doesn't work with wayland?
			// 	}

			EngineWindow window = new(windowHandle);
			Engine3.Windows.Add(window);

			Logger.Info($"Setting up {Engine3.GraphicsApi} for window...");
			switch (Engine3.GraphicsApi) {
				case GraphicsApi.OpenGL: window.SetupOpenGL(); break;
				case GraphicsApi.Vulkan: window.SetupVulkan(); break;
				case GraphicsApi.Console: throw new UnreachableException();
				default: throw new ArgumentOutOfRangeException();
			}

			return window;
		}

		private unsafe void SetupVulkan() {
			if (Engine3.VkInstance is not { } vkInstance || Engine3.GameInstance is not { } gameInstance) { throw new UnreachableException(); }

			VkSurface = VkH.CreateSurface(vkInstance, WindowHandle);
			Logger.Debug("Created surface");

			Gpus = VkH.CreateGpus(vkInstance, VkSurface.Value);
			Logger.Debug("Created GPUs");

#if DEBUG
			VkH.PrintGpus(Gpus, true);
#endif

			BestGpu = VkH.PickBestGpu(Gpus, gameInstance.VkIsGpuSuitable, gameInstance.VkRateGpuSuitability);
			Logger.Debug($"Selected GPU: {BestGpu.Name}");

			VkH.CreateLogicalDevice(BestGpu, out VkDevice vkLogicalDevice, out VkQueue vkPresentQueue);
			VkLogicalDevice = vkLogicalDevice;
			VkPresentQueue = vkPresentQueue;
			Logger.Debug("Created logical device & present queue");

			VkDeviceQueueInfo2 deviceQueueInfo2 = new();
			VkQueue vkGraphicsQueue;
			Vk.GetDeviceQueue2(VkLogicalDevice!.Value, &deviceQueueInfo2, &vkGraphicsQueue); // VkLogicalDevice shouldn't be null here
			VkGraphicsQueue = vkGraphicsQueue;
		}

		private void SetupOpenGL() => throw new NotImplementedException(); // TODO opengl

		public void TryCloseWindow() {
			if (wasCleanedUp) { return; }

			bool shouldClose = true;
			TryCloseWindowEvent?.Invoke(ref shouldClose);

			if (shouldClose) { CloseWindow(); }
		}

		public void CloseWindow(bool callEvent = true) {
			if (wasCleanedUp) { return; }

			if (callEvent) { OnCloseWindowEvent?.Invoke(); }
			Cleanup();
		}

		public void Show() => SetWindowMode(WindowMode.Normal);
		public void Hide() => SetWindowMode(WindowMode.Hidden);
		public void SetWindowMode(WindowMode windowMode) => Toolkit.Window.SetMode(WindowHandle, windowMode);

		private void Cleanup() {
			if (wasCleanedUp) {
				Logger.Warn("Attempted to cleanup a window that was already cleaned up");
				return;
			}

			switch (Engine3.GraphicsApi) {
				case GraphicsApi.Console: break;
				case GraphicsApi.OpenGL: CleanupOpenGL(); break;
				case GraphicsApi.Vulkan: CleanupVulkan(); break;
				default: throw new ArgumentOutOfRangeException();
			}

			wasCleanedUp = true;
		}

		private void CleanupOpenGL() { }

		private unsafe void CleanupVulkan() {
			if (Engine3.VkInstance is not { } vkInstance) { return; }

			if (VkSurface is { } vkSurface) {
				Vk.DestroySurfaceKHR(vkInstance, vkSurface, null);
				VkSurface = null;
			}

			if (VkLogicalDevice is { } vkLogicalDevice) {
				Vk.DestroyDevice(vkLogicalDevice, null);
				VkLogicalDevice = null;
			}
		}

		public delegate void AttemptCloseWindow(ref bool shouldCloseWindow);
	}
}