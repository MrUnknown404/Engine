using OpenTK.Graphics.Vulkan;
using Silk.NET.Shaderc;

namespace Engine4.Client.Graphics;

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

		public ShaderKind Shaderc_ShaderKind =>
				self switch {
						ShaderType.Fragment => ShaderKind.FragmentShader,
						ShaderType.Vertex => ShaderKind.VertexShader,
						ShaderType.Geometry => ShaderKind.GeometryShader,
						ShaderType.TessEvaluation => ShaderKind.TessEvaluationShader,
						ShaderType.TessControl => ShaderKind.TessControlShader,
						ShaderType.Compute => ShaderKind.ComputeShader,
						_ => throw new ArgumentOutOfRangeException(nameof(self), self, null),
				};

		public VkShaderStageFlagBits ShaderStageFlags =>
				self switch {
						ShaderType.Fragment => VkShaderStageFlagBits.ShaderStageFragmentBit,
						ShaderType.Vertex => VkShaderStageFlagBits.ShaderStageVertexBit,
						ShaderType.Geometry => VkShaderStageFlagBits.ShaderStageGeometryBit,
						ShaderType.TessEvaluation => VkShaderStageFlagBits.ShaderStageTessellationEvaluationBit,
						ShaderType.TessControl => VkShaderStageFlagBits.ShaderStageTessellationControlBit,
						ShaderType.Compute => VkShaderStageFlagBits.ShaderStageComputeBit,
						_ => throw new ArgumentOutOfRangeException(),
				};
	}
}