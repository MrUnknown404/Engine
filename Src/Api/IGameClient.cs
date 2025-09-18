using Engine3.Api.Graphics;
using Engine3.Utils;
using JetBrains.Annotations;

namespace Engine3.Api {
	[PublicAPI]
	public interface IGameClient {
		public Version4 Version { get; }
		public string StartupMessage { get; }
		public string ExitMessage { get; }

		public List<RenderLayer> RenderLayers { get; }

		public void Update();

		public void AddRenderLayer(IProgramPipeline programPipeline, RenderLayer.RenderDelegate[] renderFuncs) => RenderLayers.Add(new(programPipeline, renderFuncs));

		public bool IsCloseAllowed() => true;
	}
}