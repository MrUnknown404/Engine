namespace Engine3.Utils {
	[Obsolete]
	public enum EngineLoadState : byte {
		None = 0,
		Logger,
		Assemblies,
		Engine,
		Game,
		Graphics,
		EnginePostGraphics,
		Done = 255,
	}
}