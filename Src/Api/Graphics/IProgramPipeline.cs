namespace Engine3.Api.Graphics {
	public interface IProgramPipeline {
		public IShaderAccess? VertexShader { get; }
		public IShaderAccess? FragmentShader { get; }
		public IShaderAccess? GeometryShader { get; }
		public IShaderAccess? TessEvaluationShader { get; }
		public IShaderAccess? TessControlShader { get; }
		// TODO compute?

		public void Bind();
	}
}