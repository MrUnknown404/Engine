namespace Engine3.Client.Graphics.ImGui;

[Obsolete]
public interface IImGuiMaker<in T> {
	public static abstract void ShowImGui(T obj);
}