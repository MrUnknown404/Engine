using Engine3.Exceptions;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Engine3.Client.Graphics.OpenGL.Objects {
	public class ProgramPipeline : INamedGraphicsResource, IEquatable<ProgramPipeline> {
		public ProgramPipelineHandle Handle { get; }

		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		internal ProgramPipeline(string debugName, OpenGLShader? vert, OpenGLShader? frag, OpenGLShader? geom = null, OpenGLShader? tessEval = null, OpenGLShader? tessCtrl = null) {
			DebugName = debugName;
			Handle = new(GL.CreateProgramPipeline());

			TryAddStage(vert);
			TryAddStage(frag);
			TryAddStage(geom);
			TryAddStage(tessEval);
			TryAddStage(tessCtrl);

			INamedGraphicsResource.PrintNameWithHandle(this, Handle.Handle);

			return;

			void TryAddStage(OpenGLShader? shader) {
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

		public bool Equals(ProgramPipeline? other) => other != null && Handle == other.Handle;
		public override bool Equals(object? obj) => obj is ProgramPipeline programPipeline && Equals(programPipeline);

		public override int GetHashCode() => Handle.GetHashCode();

		public static bool operator ==(ProgramPipeline? left, ProgramPipeline? right) => Equals(left, right);
		public static bool operator !=(ProgramPipeline? left, ProgramPipeline? right) => !Equals(left, right);
	}
}