using Engine3.Exceptions;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Engine3.Client.Graphics.OpenGL.Objects {
	public sealed class ProgramPipeline : NamedGraphicsResource<ProgramPipeline, nint> {
		public ProgramPipelineHandle ProgramPipelineHandle { get; }

		protected override nint Handle => ProgramPipelineHandle.Handle;

		internal ProgramPipeline(string debugName, OpenGLShader? vert, OpenGLShader? frag, OpenGLShader? geom = null, OpenGLShader? tessEval = null, OpenGLShader? tessCtrl = null) : base(debugName) {
			ProgramPipelineHandle = new(GL.CreateProgramPipeline());

			TryAddStage(vert);
			TryAddStage(frag);
			TryAddStage(geom);
			TryAddStage(tessEval);
			TryAddStage(tessCtrl);

			PrintCreate();

			return;

			void TryAddStage(OpenGLShader? shader) {
				if (shader == null) { return; }
				if (shader.ShaderHandle.Handle == 0) { throw new Engine3OpenGLException($"Program Pipeline: {debugName}:{ProgramPipelineHandle} has an invalid shader program ({shader.DebugName}). No handle"); }
				GL.UseProgramStages((int)ProgramPipelineHandle, shader.ShaderType switch {
						ShaderType.Fragment => UseProgramStageMask.FragmentShaderBit,
						ShaderType.Vertex => UseProgramStageMask.VertexShaderBit,
						ShaderType.Geometry => UseProgramStageMask.GeometryShaderBit,
						ShaderType.TessEvaluation => UseProgramStageMask.TessEvaluationShaderBit,
						ShaderType.TessControl => UseProgramStageMask.TessControlShaderBit,
						ShaderType.Compute => UseProgramStageMask.ComputeShaderBit,
						_ => throw new ArgumentOutOfRangeException(nameof(shader.ShaderType), shader.ShaderType, null),
				}, (int)shader.ShaderHandle);
			}
		}

		protected override void Cleanup() => GL.DeleteProgramPipeline((int)ProgramPipelineHandle);
	}
}