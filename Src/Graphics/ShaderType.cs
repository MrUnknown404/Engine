using System.Diagnostics.CodeAnalysis;
using shaderc;

namespace Engine3.Graphics {
	public enum ShaderType {
		Fragment,
		Vertex,
		Geometry,
		TessEvaluation,
		TessControl,
		Compute,
	}

	public static class ShaderTypeExtensions {
		extension(ShaderType self) {
			[SuppressMessage("Performance", "CA1822:Mark members as static")] // lies
			public string FileExtension =>
					self switch {
							ShaderType.Fragment => "frag",
							ShaderType.Vertex => "vert",
							ShaderType.Geometry => "geom",
							ShaderType.TessEvaluation => "tese",
							ShaderType.TessControl => "tesc",
							ShaderType.Compute => "comp",
							_ => throw new ArgumentOutOfRangeException(nameof(self), self, null),
					};

			public ShaderKind ShaderKind =>
					self switch {
							ShaderType.Fragment => ShaderKind.FragmentShader,
							ShaderType.Vertex => ShaderKind.VertexShader,
							ShaderType.Geometry => ShaderKind.GeometryShader,
							ShaderType.TessEvaluation => ShaderKind.TessEvaluationShader,
							ShaderType.TessControl => ShaderKind.TessControlShader,
							ShaderType.Compute => ShaderKind.ComputeShader,
							_ => throw new ArgumentOutOfRangeException(nameof(self), self, null),
					};
		}
	}
}