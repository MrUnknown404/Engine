using System.Diagnostics.CodeAnalysis;
using Engine4.Client.Graphics.Rendering;
using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Utility;
using Engine4.Client.Windowing;
using Engine4.IO;
using Engine4.Utility;
using Engine4.Utility.Exceptions;
using Engine4.Utility.Math;
using Engine4.Utility.Versions;
using NLog;
using OpenTK.Graphics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ImGuiNet = Hexa.NET.ImGui.ImGui;
using Window = Engine4.Client.Windowing.Window;

namespace Engine4.Client;

// TODO https://docs.vulkan.org/tutorial/latest/Building_a_Simple_Engine/Engine_Architecture/05_rendering_pipeline.html read

public abstract class GameClient : GameCore {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Engine);

	// windowing & graphics
	public bool IsGlfwEnabled { get; private set; } // note: not set until SetupInternals()
	public bool IsVulkanEnabled { get; private set; } // ^

	private readonly List<Window> windows = new();
	private readonly List<Renderer> renderers = new();
	private readonly Dictionary<Window, Renderer> windowToRenderer = new(); // TODO window/renderer should impl HashCode

	protected VulkanManager? VulkanManager { get; private set; }
	protected GlfwManager? GlfwManager { get; private set; }

	protected bool AnyWindowsExist => windows.Count != 0;

	protected GameClient(string name, IPackableVersion version) : base(name, version) { }

	protected sealed override void SetupInternals(StartupSettings coreSettings) {
		if (coreSettings is not ClientStartupSettings clientSettings) { throw new ArgumentException($"{nameof(coreSettings)} needs to be of type {nameof(ClientStartupSettings)}"); }

		base.SetupInternals(coreSettings);

		if (clientSettings.EnableGlfw) {
			if (!clientSettings.EnableVulkan) { Logger.Warn("Glfw is enabled but Vulkan is not. Vulkan is required to draw to a Window"); }

			SetupGlfw();
		}

		if (clientSettings.EnableVulkan) {
			SetupVulkan(clientSettings.VulkanSettings ?? throw new IllegalStateException());
			SetupImGui();
		}
	}

	protected sealed override void InternalUpdate() {
		// try close windows
		for (int i = 0; i < windows.Count; i++) {
			Window window = windows[i];

			if (window.GlfwShouldClose()) { window.RequestClose(false); }
			if (!window.ShouldClose) { continue; }

			Logger.Debug("Found window to close. Closing it...");

			if (windowToRenderer.TryGetValue(window, out Renderer? renderer)) {
				Logger.Trace("- Window has a renderer. Cleaning that first...");

				renderer.Cleanup(); // calls Vk.DeviceWaitIdle()
				renderers.Remove(renderer);
			}

			window.Cleanup();
			windows.RemoveAt(i);
			i--;
		}

		// try clean up resources
		foreach (Renderer renderer in renderers) { renderer.TryCleanup(); } // can call Vk.DeviceWaitIdle()

		base.InternalUpdate();
	}

	protected sealed override void InternalRender(float delta) {
		base.InternalRender(delta); //
	}

	protected sealed override void Render(float delta) {
		foreach (Renderer renderer in renderers) { renderer.InternalRender(delta); } // TODO time individual renderers
	}

	protected void CreateWindow(string title, ushort width, ushort height, Color4 clearColor, out Window window, out Renderer renderer, Action? setWindowHints = null) {
		if (!IsGlfwEnabled) { throw new Engine4Exception($"Cannot create a {nameof(Window)} when Glfw is not enabled"); }
		if (!IsVulkanEnabled) { throw new Engine4Exception($"Cannot create a {nameof(Renderer)} when Vulkan is not enabled"); }

		if (VulkanManager == null) { throw new IllegalStateException(); }

		Logger.Debug("Creating window...");
		window = new(title, width, height, setWindowHints);

		Logger.Debug("Creating renderer...");
		renderer = new(VulkanManager ?? throw new IllegalStateException(), window, clearColor);

		windows.Add(window);
		renderers.Add(renderer);
		windowToRenderer.Add(window, renderer);
	}

	[SuppressMessage("ReSharper", "HeuristicUnreachableCode")]
	private void SetupGlfw() {
		Logger.Debug("Glfw is enabled. Loading Glfw...");
		IsGlfwEnabled = true;

		GlfwManager = new();
		PollEvents = GLFW.PollEvents;
	}

	private void SetupVulkan(SetupVulkanSettings vulkanSettings) {
		Logger.Debug("Vulkan is enabled. Loading Vulkan...");
		IsVulkanEnabled = true;

		Logger.Trace("Loading Vulkan bindings...");
		VKLoader.Init();

		// get vulkan api version
		VkH.GetApiVersion(out uint apiVersion, out _, out byte major, out ushort minor, out ushort patch);
		Logger.Debug($"- Version: {major}.{minor}.{patch} ({apiVersion})");

		VulkanManager = new(this, vulkanSettings);
	}

	private void SetupImGui() => Logger.Debug($"- ImGui Version: {ImGuiNet.GetVersionS()}");

	protected sealed override void InternalCleanup() {
		base.InternalCleanup();

		if (IsVulkanEnabled) {
			Logger.Trace("Cleaning up Vulkan...");

			if (VulkanManager == null) { throw new IllegalStateException(); } // if vulkan is enabled these should not be null

			if (IsGlfwEnabled) {
				Logger.Trace($"- Cleaning up {renderers.Count} renderers...");
				foreach (Renderer renderer in renderers) {
					renderer.Cleanup(); // calls Vk.DeviceWaitIdle()
				}
			}

			VulkanManager.Cleanup();
		}

		if (IsGlfwEnabled) {
			Logger.Trace("Cleaning up Glfw...");

			if (GlfwManager == null) { throw new IllegalStateException(); } // if glfw is enabled these should not be null

			Logger.Trace($"- Cleaning up {windows.Count} windows...");
			foreach (Window window in windows) { window.Cleanup(); }

			GlfwManager.Cleanup();
		}
	}
}