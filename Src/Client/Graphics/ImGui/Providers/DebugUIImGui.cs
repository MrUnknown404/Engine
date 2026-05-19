using System.Numerics;
using Engine3.Utility;
using ImGuiNET;
using OpenTK.Platform;

namespace Engine3.Client.Graphics.ImGui.Providers;

public class DebugUIImGui : IImGuiProvider {
	public byte IndentAmount { get; init; } = 6;

	public AddExtraDebugUIDelegate? AddExtraDebugUI { get; init; }

	private readonly GameClient game;
	private readonly PerformanceMonitor performanceMonitor;
	private readonly KeyboardManager keyboardManager;
	private readonly MouseManager mouseManager;

	private bool showUpdateIndex;
	private bool showUps = true;
	private bool showUpdateTime = true;
	private bool showUpdateTimeGraph;
	private bool showMinMaxAvgUpdateTime;

	private bool showFrameIndex;
	private bool showFps = true;
	private bool showFrameTime = true;
	private bool showFrameTimeGraph;
	private bool showMinMaxAvgFrameTime;

	private bool showBackendSettings;
	private bool popoutUpdates;
	private bool popoutFrames;

	public DebugUIImGui(GameClient game, Window window) {
		this.game = game;
		performanceMonitor = game.PerformanceMonitor;
		keyboardManager = window.KeyboardManager;
		mouseManager = window.MouseManager;
	}

	public void ShowImGui() {
		bool showAnyUpdates = showUpdateIndex || showUps || showUpdateTime || showMinMaxAvgUpdateTime;
		bool showAnyFrames = showFrameIndex || showFps || showFrameTime || showMinMaxAvgFrameTime;

		if (ImGuiNet.Begin("Debug")) {
			ImGuiH.IndentedCollapsingHeader("Performance", IndentAmount, ShowPerformance, ImGuiTreeNodeFlags.DefaultOpen);
			ImGuiH.IndentedCollapsingHeader("Input", IndentAmount, ShowInput);

			if (AddExtraDebugUI != null) {
				ImGuiNet.Separator();
				AddExtraDebugUI.Invoke(IndentAmount);
			}
		}

		ImGuiNet.End();

		if (showAnyUpdates && popoutUpdates) {
			ImGuiNet.Begin("Update Info");
			ShowUpdate();
			ImGuiNet.End();
		}

		if (showAnyFrames && popoutFrames) {
			ImGuiNet.Begin("Frame Info");
			ShowFrame();
			ImGuiNet.End();
		}

		return;

		void ShowPerformance() {
			if (!popoutUpdates && showAnyUpdates) {
				ImGuiNet.SeparatorText("Update Info");
				ShowUpdate();
			}

			if (!popoutFrames && showAnyFrames) {
				ImGuiNet.SeparatorText("Frame Info");
				ShowFrame();
			}

			// TODO show resource usage

			if (showBackendSettings) { ShowBackendSettings(); }

			if ((!popoutUpdates && showAnyUpdates) || (!popoutFrames && showAnyFrames) || showBackendSettings) { ImGuiNet.Separator(); }

			ImGuiH.IndentedCollapsingHeader("Toggles", IndentAmount, ShowToggles);
		}

		void ShowUpdate() {
			if (showUpdateIndex) { ImGuiNet.Text($"Index: {game.UpdateIndex}"); }
			if (showUps) { ImGuiNet.Text($"Ups: {performanceMonitor.Ups}/{game.TargetUps}"); }
			if (showUpdateTime) { ImGuiNet.Text($"Time: {performanceMonitor.UpdateTime:F3} ms"); }

			if (showUpdateTimeGraph) {
				if (performanceMonitor.StoreTimesForGraph) {
					float[] times = performanceMonitor.LastUpdateTimes;
					if (times.Length != 0) { ImGuiNet.PlotLines("Update Time Graph", ref times[0], times.Length); }
				} else { ImGuiNet.Text($"{nameof(performanceMonitor.StoreTimesForGraph)} is false"); }
			}

			if (showMinMaxAvgUpdateTime) {
				if (performanceMonitor.CalculateMinMaxAverage) {
					ImGuiNet.Text($"Min: {performanceMonitor.MinUpdateTime:F3} ms");
					ImGuiNet.Text($"Max: {performanceMonitor.MaxUpdateTime:F3} ms");
					ImGuiNet.Text($"Avg: {performanceMonitor.AvgUpdateTime:F3} ms");
				} else { ImGuiNet.Text($"{nameof(performanceMonitor.CalculateMinMaxAverage)} is false"); }
			}
		}

		void ShowFrame() {
			if (showFrameIndex) { ImGuiNet.Text($"Index: {game.FrameIndex}"); }
			if (showFps) { ImGuiNet.Text($"Fps: {performanceMonitor.Fps}{(game.TargetFps == 0 ? string.Empty : $"/{game.TargetFps}")}"); }
			if (showFrameTime) { ImGuiNet.Text($"Time: {performanceMonitor.FrameTime:F3} ms"); }

			if (showFrameTimeGraph) {
				if (performanceMonitor.StoreTimesForGraph) {
					float[] times = performanceMonitor.LastFrameTimes;
					if (times.Length != 0) { ImGuiNet.PlotLines("Frame Time Graph", ref times[0], times.Length); }
				} else { ImGuiNet.Text($"{nameof(performanceMonitor.StoreTimesForGraph)} is false"); }
			}

			if (showMinMaxAvgFrameTime) {
				if (performanceMonitor.CalculateMinMaxAverage) {
					ImGuiNet.Text($"Min: {performanceMonitor.MinFrameTime:F3} ms");
					ImGuiNet.Text($"Max: {performanceMonitor.MaxFrameTime:F3} ms");
					ImGuiNet.Text($"Avg: {performanceMonitor.AvgFrameTime:F3} ms");
				} else { ImGuiNet.Text($"{nameof(performanceMonitor.CalculateMinMaxAverage)} is false"); }
			}
		}

		void ShowBackendSettings() {
			ImGuiNet.SeparatorText("Backend Settings");

			ImGuiNet.Text($"Calculate Min/Max/Avg: {performanceMonitor.CalculateMinMaxAverage}");
			ImGuiNet.Text($"Min/Max/Avg Sample Time: {performanceMonitor.MinMaxAverageSampleTime} seconds");
			ImGuiNet.Text($"Store Times For Graph: {performanceMonitor.StoreTimesForGraph}");
			ImGuiNet.Text($"Update Time Graph Size: {performanceMonitor.LastUpdateTimeSize}");
			ImGuiNet.Text($"Frame Time Graph Size: {performanceMonitor.LastFrameTimeSize}");
		}

		void ShowToggles() {
			ImGuiNet.Checkbox("Show Update Index", ref showUpdateIndex);
			ImGuiNet.Checkbox("Show Ups", ref showUps);
			ImGuiNet.Checkbox("Show Update Time", ref showUpdateTime);
			ImGuiNet.Checkbox("Show Update Time Graph", ref showUpdateTimeGraph);
			ImGuiNet.Checkbox("Show Min/Max/Avg Update Time", ref showMinMaxAvgUpdateTime);

			ImGuiNet.Separator();
			ImGuiNet.Checkbox("Show Frame Index", ref showFrameIndex);
			ImGuiNet.Checkbox("Show Fps", ref showFps);
			ImGuiNet.Checkbox("Show Frame Time", ref showFrameTime);
			ImGuiNet.Checkbox("Show Frame Time Graph", ref showFrameTimeGraph);
			ImGuiNet.Checkbox("Show Min/Max/Avg Frame Time", ref showMinMaxAvgFrameTime);

			ImGuiNet.Separator();
			ImGuiNet.Checkbox("Show Backend Settings", ref showBackendSettings);
			ImGuiNet.Checkbox("Popout Updates", ref popoutUpdates);
			ImGuiNet.Checkbox("Popout Frames", ref popoutFrames);
		}

		void ShowInput() {
			ImGuiH.IndentedCollapsingHeader("Mouse", IndentAmount, ShowMouse);
			ImGuiH.IndentedCollapsingHeader("Keyboard", IndentAmount, ShowKeyboard);
		}

		void ShowMouse() {
			Vector2 position = mouseManager.Position;
			ImGuiNet.InputFloat2("Position", ref position, "%.1f");
			ImGuiH.HelpMarker("X/Y");

			ImGuiNet.Text($"Scroll Delta: {mouseManager.ScrollDelta:F1}");
			ImGuiNet.Text($"Scroll Amount: {mouseManager.ScrollAmount:F3}");

			foreach (MouseButton button in Enum.GetValues<MouseButton>()) {
				bool b = mouseManager.IsButton(button);
				ImGuiNet.Checkbox($"{button}", ref b);
			}
		}

		void ShowKeyboard() {
			ImGuiH.IndentedCollapsingHeader("Active Keys", IndentAmount, ShowActiveKeys);
			ImGuiH.IndentedCollapsingHeader("Inactive Keys", IndentAmount, ShowInactiveKeys);
		}

		void ShowActiveKeys() {
			foreach (Key key in Enum.GetValues<Key>()) {
				bool isKey = keyboardManager.IsKey(key);
				if (isKey) { ImGuiNet.Checkbox($"{key}", ref isKey); }
			}
		}

		void ShowInactiveKeys() {
			foreach (Key key in Enum.GetValues<Key>()) {
				bool isKey = keyboardManager.IsKey(key);
				if (!isKey) { ImGuiNet.Checkbox($"{key}", ref isKey); }
			}
		}
	}

	public delegate void AddExtraDebugUIDelegate(float indentAmount);
}