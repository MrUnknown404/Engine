namespace Engine3.Client.Graphics.ImGui.Providers;

public class DemoWindowImGui : IImGuiProvider {
	public void ShowImGui() => ImGuiNet.ShowDemoWindow();
}