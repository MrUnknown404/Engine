using Engine3.Exceptions;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Engine3.Client.Graphics.OpenGL {
	public class ProgramPipeline : IGraphicsResource {
		public ProgramPipelineHandle Handle { get; }

		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		public ProgramPipeline(string debugName, GlShader? vert, GlShader? frag, GlShader? geom = null, GlShader? tessEval = null, GlShader? tessCtrl = null) {
			DebugName = debugName;
			Handle = new(GL.CreateProgramPipeline());

			TryAddStage(vert);
			TryAddStage(frag);
			TryAddStage(geom);
			TryAddStage(tessEval);
			TryAddStage(tessCtrl);

			return;

			void TryAddStage(GlShader? shader) {
				if (shader == null) { return; }
				if (shader.Handle.Handle == 0) { throw new Engine3OpenGLException($"Program Pipeline: {debugName}:{Handle} has an invalid shader program ({shader.DebugName}). No handle"); }
				GL.UseProgramStages((int)Handle, shader.ShaderType switch {
						ShaderType.Fragment => UseProgramStageMask.FragmentShaderBit,
						ShaderType.Vertex => UseProgramStageMask.VertexShaderBit,
						ShaderType.Geometry => UseProgramStageMask.GeometryShaderBit,
						ShaderType.TessEvaluation => UseProgramStageMask.TessEvaluationShaderBit,
						ShaderType.TessControl => UseProgramStageMask.TessControlShaderBit,
						ShaderType.Compute => UseProgramStageMask.ComputeShaderBit,
						_ => throw new ArgumentOutOfRangeException(nameof(shader.ShaderType), shader.ShaderType, null),
				}, (int)shader.Handle);
			}
		}

		public void Destroy() {
			if (IGraphicsResource.WarnIfDestroyed(this)) { return; }

			GL.DeleteProgramPipeline((int)Handle);

			WasDestroyed = true;
		}
	}
}