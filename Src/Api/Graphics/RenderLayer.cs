namespace Engine3.Api.Graphics {
	public class RenderLayer { // TODO this is probably going to need major reworking once vulkan is implemented
		private readonly IProgramPipeline programPipeline;
		private readonly RenderDelegate renderFunc;

		public RenderLayer(IProgramPipeline programPipeline, RenderDelegate renderFunc) {
			this.programPipeline = programPipeline;
			this.renderFunc = renderFunc;
		}

		public void Render(float delta) {
			programPipeline.Bind();
			renderFunc(programPipeline, delta);
		}

		public delegate void RenderDelegate(IProgramPipeline programPipeline, float delta);
	}
}