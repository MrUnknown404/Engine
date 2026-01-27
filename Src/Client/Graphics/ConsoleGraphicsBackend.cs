namespace Engine3.Client.Graphics {
	public class ConsoleGraphicsBackend : EngineGraphicsBackend {
		public ConsoleGraphicsBackend() : base(GraphicsBackend.Console, null) { }

		protected internal override void Setup(GameClient gameClient) { }
		protected internal override void Cleanup() { }
	}
}