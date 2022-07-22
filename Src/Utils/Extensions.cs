using OpenTK.Graphics.OpenGL4;

namespace USharpLibs.Engine.Utils {
	public static class ShaderTypeExtension {
		public static string ToFileFormat<T>(this ShaderType self) => self switch {
			ShaderType.FragmentShader => "frag",
			ShaderType.VertexShader => "vert",
			ShaderType.GeometryShader => "geom",
			ShaderType.TessEvaluationShader => "tese",
			ShaderType.TessControlShader => "tesc",
			ShaderType.ComputeShader => "comp",
			_ => throw new NotImplementedException(),
		};
	}
}