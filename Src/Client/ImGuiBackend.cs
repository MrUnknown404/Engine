using ImGuiNET;

namespace Engine3.Client {
	public abstract class ImGuiBackend {
		public abstract bool NewFrame(out ImDrawDataPtr imDrawData);
		public abstract void UpdateBuffers(ImDrawDataPtr imDrawData);
	}
}