using System.Diagnostics.CodeAnalysis;
using Engine3.Client.Client.Graphics.DataStructs;
using Engine3.Client.Client.Graphics.VertexLayouts;
using Engine3.Client.Client.ImGui;
using Engine3.Client.Utility;
using Engine3.Core;
using Engine3.Core.Client;
using Engine3.Core.Debug;
using Engine3.Core.Utility.Exceptions;
using ImGuiNET;
using NLog;
using OpenTK.Graphics.Vulkan;
using OpenTK.Platform;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Silk.NET.Core.Loader;
using Silk.NET.Shaderc;
using StbiSharp;
using GraphicsApi = Engine3.Core.Utility.GraphicsApi;
using GraphicsBackend = Engine3.Client.Client.Graphics.GraphicsBackend;
using Window = Engine3.Client.Client.Window;

namespace Engine3.Client;

public sealed class Engine3Client : Core.Engine3 {
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	[field: MaybeNull]
	internal Shaderc Shaderc => field ?? throw new Engine3Exception($"Attempted to get {nameof(Shaderc)} using an unsupported graphics backend ({GraphicsBackend})");

	public GraphicsBackend? GraphicsBackend { get; }

	public bool StbiFlipOnLoad { get; init; } = true;

	private readonly List<Window> windows = new();
	private readonly Dictionary<Window, Renderer> windowToRendererDict = new();
	private readonly Queue<Window> windowCloseQueue = new();

	public event OnOpenTKSetupDoneDelegate? OnOpenTKSetupDoneEvent;

	public Engine3Client(GraphicsBackend? graphicsBackend) : base(typeof(Engine3Client).Assembly, graphicsBackend?.GraphicsApi ?? GraphicsApi.None) {
		GraphicsBackend = graphicsBackend;

		if (graphicsBackend?.GraphicsApi is GraphicsApi.Vulkan or GraphicsApi.OpenGL) { Shaderc = new(Shaderc.CreateDefaultContext(new ShadercSearchPathContainer().GetLibraryNames())); }

#if DEBUG
		// vertices
		StructLayoutDumper.AddStruct<VertexXyz>();
		StructLayoutDumper.AddStruct<VertexXyzUv>();
		StructLayoutDumper.AddStruct<VertexXyzRgb>();
		StructLayoutDumper.AddStruct<VertexXyzUvRgb>();

		// materials
		StructLayoutDumper.AddStruct<Material>();

		//
		StructLayoutDumper.AddStruct<ProjectionView>();
		StructLayoutDumper.AddStruct<ProjectionModel>();
#endif
	}

	protected override void PrintEngineSettings() {
		base.PrintEngineSettings();
		Logger.Debug($"- GLFW Version: {GLFW.GetVersionString()}"); // TODO i have no idea what window manager OpenTK uses. i see GLFW & SDL. but it looks like PAL is just using Win32 API/X11 API directly. help

		uint spvVersion = 0, spvRevision = 0;
		Shaderc.GetSpvVersion(ref spvVersion, ref spvRevision);
		Logger.Debug($"- SpirV Version: {(spvVersion & 16711680) >> 16}.{(spvVersion & 65280) >> 8} - {spvRevision}");
		Logger.Debug($"- ImGui Version: {ImGui.GetVersion()}");
	}

	protected override void SetupPreGraphics() {
		base.SetupPreGraphics();
		Stbi.SetFlipVerticallyOnLoad(StbiFlipOnLoad);
	}

	protected override void SetupConsoleGraphics(EngineGame game) { throw new NotImplementedException(); } // TODO impl

	protected override void SetupOpenGLGraphics(EngineGame game) {
		SetupOpenTK(new() { ApplicationName = Name, Logger = new TkLogger(), FeatureFlags = ToolkitFlags.EnableOpenGL, });
		GraphicsBackend!.Setup(game); // shouldn't be null here
	}

	protected override void SetupVulkanGraphics(EngineGame game) {
		SetupOpenTK(new() { ApplicationName = Name, Logger = new TkLogger(), FeatureFlags = ToolkitFlags.EnableVulkan, });
		GraphicsBackend!.Setup(game); // shouldn't be null here
	}

	protected override void TryProcessEvents() {
		if (GraphicsBackend?.GraphicsApi is GraphicsApi.OpenGL or GraphicsApi.Vulkan) { Toolkit.Window.ProcessEvents(false); }
	}

	protected override void TryCleanupBeforeRendering() {
		base.TryCleanupBeforeRendering();

		foreach (Window window2 in Enumerable.Where(windows, static window => window.ShouldClose)) {
			Logger.Debug("Found window to destroy...");
			windowCloseQueue.Enqueue(window2);
		}

		while (windowCloseQueue.TryDequeue(out Window? window)) { RemoveWindow(window); }
	}

	protected override void UpdateCleanup() {
		base.UpdateCleanup();
		foreach (Window window in windows) { window.MouseManager.ResetScroll(); }
	}

	protected override void RenderCleanup() {
		base.RenderCleanup();
		ImGuiH.ResetWidgetOffset();
	}

	public void AddWindow<T>(T window, Renderer renderer) where T : Window {
		if (GraphicsBackend?.GraphicsApi is not GraphicsApi.Vulkan and GraphicsApi.OpenGL) {
			Logger.Warn("Cannot add windows when using None/Console graphics backend");
			return;
		}

		Logger.Trace("Window added");
		windows.Add(window);
		windowToRendererDict.Add(window, renderer);
	}

	private void RemoveWindow<T>(T window) where T : Window {
		if (windows.Remove(window)) {
			if (windowToRendererDict.TryGetValue(window, out Renderer? renderer)) {
				RemoveRenderer(renderer);
				windowToRendererDict.Remove(window);
			}

			Logger.Debug($"Destroying {nameof(Window)}...");
			window.Destroy();
		} else { Logger.Error($"Could not find to be destroyed {nameof(Window)} in {nameof(Core.Engine3)}'s {nameof(Window)} list"); }
	}

	private void SetupOpenTK(ToolkitOptions toolkitOptions) {
		EventQueue.EventRaised += OnEventRaised;

		Toolkit.Init(toolkitOptions);

		OnOpenTKSetupDoneEvent?.Invoke();
	}

	private void OnEventRaised(PalHandle? palHandle, PlatformEventType platformEventType, EventArgs args) { // TODO merge ImGUI events into here
		if (args is WindowEventArgs windowArgs) {
			if (!FindWindow(windowArgs.Window, out Window? window)) {
				Logger.Warn("EventQueue received on an unknown window");
				return;
			}

			switch (args) {
				case CloseEventArgs: window.OnCloseEventArgs(); break;
				case WindowResizeEventArgs resizeArgs: window.OnResizeEventArgs(resizeArgs); break;
				case WindowModeChangeEventArgs modeArgs: window.OnModeChangeEventArgs(modeArgs); break;
				case KeyDownEventArgs downArgs: window.OnKeyDownEventArgs(downArgs); break;
				case KeyUpEventArgs upArgs: window.OnKeyUpEventArgs(upArgs); break;
				case MouseMoveEventArgs moveArgs: window.OnMouseMoveEventArgs(moveArgs); break;
				case MouseButtonDownEventArgs downArgs: window.OnMouseButtonDownEventArgs(downArgs); break;
				case MouseButtonUpEventArgs upArgs: window.OnMouseButtonUpEventArgs(upArgs); break;
				case ScrollEventArgs scrollArgs: window.OnScrollEventArgs(scrollArgs); break;
			}
		} else {
			// ignored atm
		}

		return;

		bool FindWindow(WindowHandle windowHandle, [NotNullWhen(true)] out Window? window) => (window = windows.Find(w => w.WindowHandle == windowHandle)) != null;
	}

	protected override void CleanupEngine() {
		base.CleanupEngine();

		if (GraphicsBackend?.GraphicsApi is GraphicsApi.Vulkan or GraphicsApi.OpenGL) {
			EventQueue.EventRaised -= OnEventRaised;

			Logger.Debug($"Cleaning up {windows.Count} {nameof(Window)}s...");
			foreach (Window window in windows) { window.Destroy(); }

			Logger.Debug("Cleaning up ImGui...");
			ImGuiH.Cleanup();

			Shaderc.Dispose();

			Toolkit.Uninit();
		}
	}

	protected override void CleanupGraphics() {
		if (GraphicsBackend?.GraphicsApi is GraphicsApi.Vulkan or GraphicsApi.OpenGL) {
			GraphicsBackend.Cleanup(); //
		}
	}

	public delegate void OnOpenTKSetupDoneDelegate();

	private class ShadercSearchPathContainer : SearchPathContainer {
		public override string[] Linux => new[] { "libshaderc_shared.so", "libshaderc.so", };
		public override string[] MacOS => new[] { "libshaderc_shared.dylib", };
		public override string[] Android => new[] { "libshaderc_shared.so", };
		public override string[] IOS => new[] { string.Empty, };
		public override string[] Windows64 => new[] { "shaderc_shared.dll", };
		public override string[] Windows86 => new[] { "shaderc_shared.dll", };
	}

	public static class VulkanDefaults {
		internal static readonly string[] RequiredValidationLayers = [
#if DEBUG
				"VK_LAYER_KHRONOS_validation", // if OpenTK defines this somewhere, i could not find it
#endif
		];

		internal static readonly string[] RequiredInstanceExtensions = [
				Vk.KhrGetSurfaceCapabilities2ExtensionName,
#if DEBUG
				Vk.ExtDebugUtilsExtensionName,
#endif
		];

		internal static readonly string[] RequiredDeviceExtensions = [ Vk.KhrSwapchainExtensionName, Vk.KhrDynamicRenderingExtensionName, ];
	}

	public static class OpenGLDefaults;
}