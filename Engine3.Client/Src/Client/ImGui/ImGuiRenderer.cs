using Engine3.Client.Client.Graphics;
using ImGuiNET;

namespace Engine3.Client.Client.ImGui;

public abstract class ImGuiRenderer {
	protected const string ImGuiAssetName = "ImGui";

	protected abstract IGraphicsResourceProvider GraphicsResourceProvider { get; }
	protected ImGuiBackend ImGuiBackend { get; }

	protected ImGuiRenderer(ImGuiBackend imGuiBackend) => ImGuiBackend = imGuiBackend;

	protected internal abstract void CopyBuffers(ImDrawDataPtr drawData);
	protected internal abstract void DrawFrame(ImDrawDataPtr drawData);
}