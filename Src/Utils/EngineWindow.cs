using System.Diagnostics;
using Engine3.Exceptions;
using Engine3.Graphics.Vulkan;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;
using OpenTK.Platform;
using GraphicsApi = Engine3.Graphics.GraphicsApi;

namespace Engine3.Utils {
	public class EngineWindow { // TODO VulkanWindow & OpenGLWindow class
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public WindowHandle WindowHandle { get; }

		public VkSurfaceKHR? VkSurface { get; private set; } // TODO look into removing nullability from these fields if i can
		public PhysicalGpu[] AvailableGpus { get; private set; } = Array.Empty<PhysicalGpu>();
		public PhysicalGpu? BestGpu { get; private set; }
		public VkDevice? VkLogicalDevice { get; private set; } // TODO LogicalGpu class?
		public VkQueue? VkGraphicsQueue { get; private set; }
		public VkQueue? VkPresentQueue { get; private set; }
		public VkSwapchainKHR? VkSwapChain { get; private set; }
		public VkImage[] VkSwapChainImages { get; private set; } = Array.Empty<VkImage>();
		public VkFormat? VkSwapChainImageFormat { get; private set; }
		public VkExtent2D? VkSwapChainExtent { get; private set; }
		public VkImageView[] VkSwapChainImageViews { get; private set; } = Array.Empty<VkImageView>();

		public event AttemptCloseWindow? TryCloseWindowEvent;
		public event Action? OnCloseWindowEvent;

		private bool wasCleanedUp;

		private EngineWindow(WindowHandle windowHandle) => WindowHandle = windowHandle;

		[MustUseReturnValue]
		public static EngineWindow CreateWindow(string title, int w, int h) {
			if (Engine3.GraphicsApi == GraphicsApi.Console) { throw new Engine3Exception("Cannot create window when using Console graphics api"); }
			if (!Engine3.WasGraphicsApiSetup) { throw new Engine3Exception("Cannot create a window with no graphics api setup"); }

			WindowHandle windowHandle = Toolkit.Window.Create(Engine3.GraphicsApiHints!); // Engine3.GraphicsApiHints shouldn't be null here
			Logger.Info($"Created new window. Using Api: {Engine3.GraphicsApi}");

			Toolkit.Window.SetTitle(windowHandle, title);
			Toolkit.Window.SetSize(windowHandle, new(w, h));

			// 	if (settings.CenterWindow) {
			// 		DisplayHandle handle = Toolkit.Display.OpenPrimary(); // TODO figure out if this is what i am supposed to do
			// 		Toolkit.Display.GetResolution(handle, out int width, out int height);
			// 		Toolkit.Window.SetPosition(windowHandle, new(width / 2 - DefaultWidth / 2, height / 2 - DefaultHeight / 2)); // doesn't work with wayland?
			// 	}

			EngineWindow window = new(windowHandle);
			Engine3.Windows.Add(window);

			Logger.Debug($"Setting up {Engine3.GraphicsApi} window...");
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

			AvailableGpus = VkH.GetValidGpus(Engine3.VkPhysicalDevices, VkSurface.Value, gameInstance.VkIsPhysicalDeviceSuitable);
			if (AvailableGpus.Length == 0) { throw new VulkanException("Could not find any GPUs"); }
			Logger.Debug("Obtained surface capable GPUs ");
			VkH.PrintGpus(AvailableGpus, Engine3.Debug);

			BestGpu = VkH.PickBestGpu(AvailableGpus, gameInstance.VkRateGpuSuitability);
			if (BestGpu == null) { throw new VulkanException("Could not find any suitable GPUs"); }
			Logger.Debug($"- Selected Gpu: {BestGpu.Name}");

			VkH.CreateLogicalDevice(BestGpu, out VkDevice vkLogicalDevice, out VkQueue vkPresentQueue);
			VkLogicalDevice = vkLogicalDevice;
			VkPresentQueue = vkPresentQueue;
			Logger.Debug("Created logical device & present queue");

			VkDeviceQueueInfo2 deviceQueueInfo2 = new();
			VkQueue vkGraphicsQueue;
			Vk.GetDeviceQueue2(VkLogicalDevice.Value, &deviceQueueInfo2, &vkGraphicsQueue);
			VkGraphicsQueue = vkGraphicsQueue;
			Logger.Debug("Obtained graphics queue");

			VkH.CreateSwapChain(WindowHandle, VkSurface.Value, BestGpu, VkLogicalDevice.Value, out VkSwapchainKHR vkSwapChain, out VkSurfaceFormat2KHR vkSurfaceFormat, out VkExtent2D vkExtent);
			VkSwapChain = vkSwapChain;
			VkSwapChainImageFormat = vkSurfaceFormat.surfaceFormat.format;
			VkSwapChainExtent = vkExtent;
			Logger.Debug("Created swap chain");

			VkSwapChainImages = VkH.GetSwapChainImages(VkLogicalDevice.Value, VkSwapChain.Value);
			Logger.Debug("Obtained swap chain images");

			VkSwapChainImageViews = VkH.CreateImageViews(VkLogicalDevice.Value, VkSwapChainImages, VkSwapChainImageFormat.Value);
			Logger.Debug("Created swap chain image views");
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

			if (!Toolkit.Window.IsWindowDestroyed(WindowHandle)) { Toolkit.Window.Destroy(WindowHandle); } else { Logger.Warn("Tried to destroy an already destroyed window"); }

			wasCleanedUp = true;
		}

		private void CleanupOpenGL() { }

		private unsafe void CleanupVulkan() {
			if (Engine3.VkInstance is not { } vkInstance) { return; }

			if (VkLogicalDevice is { } vkLogicalDevice) {
				if (VkSwapChain is { } vkSwapchain) {
					Vk.DestroySwapchainKHR(vkLogicalDevice, vkSwapchain, null);
					VkSwapChain = null;
				}

				if (VkSwapChainImageViews.Length != 0) {
					foreach (VkImageView vkImageView in VkSwapChainImageViews) { Vk.DestroyImageView(vkLogicalDevice, vkImageView, null); }
					VkSwapChainImageViews = Array.Empty<VkImageView>();
				}

				Vk.DestroyDevice(vkLogicalDevice, null);
				VkLogicalDevice = null;
			}

			if (VkSurface is { } vkSurface) {
				Vk.DestroySurfaceKHR(vkInstance, vkSurface, null);
				VkSurface = null;
			}
		}

		public delegate void AttemptCloseWindow(ref bool shouldCloseWindow);
	}
}