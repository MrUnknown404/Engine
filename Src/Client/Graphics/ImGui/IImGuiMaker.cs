namespace Engine3.Client.Graphics.ImGui {
	public interface IImGuiMaker<in T> {
		public static abstract void ShowImGui(T obj);
	}
}