using Engine3.Core;
using Engine3.Core.Utility;

namespace Engine3.Server;

public sealed class Engine3Server : Core.Engine3 {
	public Engine3Server(bool useConsoleGraphics) : base(typeof(Engine3Server).Assembly, useConsoleGraphics ? GraphicsApi.Console : GraphicsApi.None) { }

	protected override void SetupConsoleGraphics(EngineGame game) { throw new NotImplementedException(); } // TODO impl
	protected override void SetupOpenGLGraphics(EngineGame game) => throw new NotImplementedException();
	protected override void SetupVulkanGraphics(EngineGame game) => throw new NotImplementedException();

	protected override void TryProcessEvents() { }

	protected override void CleanupGraphics() { throw new NotImplementedException(); }
}