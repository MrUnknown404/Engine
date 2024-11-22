using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.OpenGL4;

namespace USharpLibs.Engine2.Client.Shaders {
	[Flags]
	public enum ShaderTypes : byte {
		Vertex = 1 << 0,
		TesselationControl = 1 << 1,
		TesselationEvaluation = 1 << 2,
		Geometry = 1 << 3,
		Fragment = 1 << 4,
		//Compute = 1 << 5, // I have no idea if this is related to what i want to use shaders for
	}

	[PublicAPI]
	public static class ShaderTypeExtension {
		[SuppressMessage("ReSharper", "SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault")]
		public static string ToFileFormat(this ShaderType self) =>
				self switch {
						ShaderType.VertexShader => "vert",
						ShaderType.TessControlShader => "tesc",
						ShaderType.TessEvaluationShader => "tese",
						ShaderType.GeometryShader => "geom",
						ShaderType.FragmentShader => "frag",
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

		[SuppressMessage("ReSharper", "SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault")]
		public static byte ToIndex(this ShaderType self) =>
				self switch {
						ShaderType.VertexShader => 0,
						ShaderType.TessControlShader => 1,
						ShaderType.TessEvaluationShader => 2,
						ShaderType.GeometryShader => 3,
						ShaderType.FragmentShader => 4,
						_ => throw new NotImplementedException(),
				};
	}
}