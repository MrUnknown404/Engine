using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;

namespace USharpLibs.Engine.Client.GL.Shaders {
	[PublicAPI]
	public static class ShaderTypeExtension {
		public static string ToFileFormat(this ShaderTypes self) =>
				self switch {
						ShaderTypes.Vertex => "vert",
						ShaderTypes.TesselationControl => "tesc",
						ShaderTypes.TesselationEvaluation => "tese",
						ShaderTypes.Geometry => "geom",
						ShaderTypes.Fragment => "frag",
						_ => throw new NotImplementedException(),
				};

		public static ShaderType ToOpenTKShader(this ShaderTypes self) =>
				self switch {
						ShaderTypes.Vertex => ShaderType.VertexShader,
						ShaderTypes.TesselationControl => ShaderType.TessControlShader,
						ShaderTypes.TesselationEvaluation => ShaderType.TessEvaluationShader,
						ShaderTypes.Geometry => ShaderType.GeometryShader,
						ShaderTypes.Fragment => ShaderType.FragmentShader,
						_ => throw new NotImplementedException(),
				};
	}
}