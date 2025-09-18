namespace Engine3.Api.Graphics {
	public class RenderLayer { // TODO this is probably going to need major reworking once vulkan is implemented
		private readonly IProgramPipeline programPipeline;
		private readonly RenderDelegate[] renderFuncs;

		public RenderLayer(IProgramPipeline programPipeline, RenderDelegate[] renderFuncs) {
			this.programPipeline = programPipeline;
			this.renderFuncs = renderFuncs;
		}

		public void Render(float delta) {
			programPipeline.Bind();
			foreach (RenderDelegate render in renderFuncs) { render(programPipeline, delta); }
		}

		public delegate void RenderDelegate(IProgramPipeline programPipeline, float delta);
	}
}