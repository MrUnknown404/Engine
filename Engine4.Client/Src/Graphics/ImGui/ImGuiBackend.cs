using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Hexa.NET.ImGui;
using JetBrains.Annotations;
using USharpLibs.Common.Math;
using ImGuiNet = Hexa.NET.ImGui.ImGui;

namespace Engine4.Client.Graphics.ImGui;

// TODO read https://github.com/ocornut/imgui/blob/master/docs/BACKENDS.md

public unsafe class ImGuiBackend {
	private readonly ImGuiContextPtr context;
	private readonly ImGuiIOPtr imGuiIO;
	private readonly ImGuiPlatformIOPtr platformIO;
	private readonly ImGuiViewportPtr mainViewport;

	internal ImGuiBackend(Window window) {
		context = ImGuiNet.CreateContext();
		ImGuiNet.SetCurrentContext(context);

		imGuiIO = ImGuiNet.GetIO();
		platformIO = ImGuiNet.GetPlatformIO();
		mainViewport = ImGuiNet.GetMainViewport();

		ref ImGuiConfigFlags configFlags = ref imGuiIO.ConfigFlags;
		configFlags |= ImGuiConfigFlags.NavEnableKeyboard;
		configFlags |= ImGuiConfigFlags.NavEnableGamepad;
		configFlags |= ImGuiConfigFlags.DockingEnable;
		// configFlags |= ImGuiConfigFlags.ViewportsEnable; // TODO
		// configFlags |= ImGuiConfigFlags.IsSrgb; // TODO

		ref ImGuiBackendFlags backendFlags = ref imGuiIO.BackendFlags;
		// backendFlags |= ImGuiBackendFlags.HasMouseCursors; // TODO
		// backendFlags |= ImGuiBackendFlags.RendererHasVtxOffset; // TODO
		backendFlags |= ImGuiBackendFlags.RendererHasTextures; // TODO
		// backendFlags |= ImGuiBackendFlags.RendererHasViewports; // TODO
		// backendFlags |= ImGuiBackendFlags.PlatformHasViewports; // TODO
		// backendFlags |= ImGuiBackendFlags.HasMouseHoveredViewport; // TODO

		imGuiIO.BackendPlatformName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Encoding.UTF8.GetBytes($"{Engine4.Name}_impl_glfw".ToLower())));
		imGuiIO.BackendRendererName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Encoding.UTF8.GetBytes($"{Engine4.Name}_impl_vulkan".ToLower())));

		imGuiIO.Fonts.AddFontDefault();

		ImGuiNet.StyleColorsDark();

		Resize(window);
	}

	[MustUseReturnValue]
	internal bool NewFrame([NotNullWhen(true)] out ImDrawDataPtr? imDrawData) {
		SetContext();

		UpdateMouse();
		UpdateCursor();

		ImGuiNet.NewFrame();

		if ((imGuiIO.ConfigFlags & ImGuiConfigFlags.DockingEnable) != 0) { ImGuiNet.DockSpaceOverViewport(0, null, ImGuiDockNodeFlags.PassthruCentralNode); }

		CallImGuiFunctions();

		ImGuiNet.EndFrame();

		ImGuiNet.Render();

		imDrawData = ImGuiNet.GetDrawData();
		return imDrawData is { Valid: true, CmdLists.Size: > 0, };
	}

	private void UpdateMouse() {
		//
	}

	private void UpdateCursor() {
		//
	}

	private void CallImGuiFunctions() {
		ImGuiNet.ShowDemoWindow(); // TODO
	}

	internal void SetContext() => ImGuiNet.SetCurrentContext(context);

	internal void Resize(Window window) {
		Vec2<ushort> framebufferSize = window.GetFrameBufferSize();
		imGuiIO.DisplaySize = new(framebufferSize.X, framebufferSize.Y);
	}

	internal void Cleanup() => ImGuiNet.DestroyContext(context);
}