using Silk.NET.Shaderc;

namespace Engine4.Client.Graphics;

[Obsolete("This should be replaced with Slang. See https://shader-slang.org/")]
public enum ShaderLanguage {
	Glsl,
	[Obsolete("See https://github.com/google/shaderc#hlsl-deprecation")]
	Hlsl,
	SpirV,
}

public static class ShaderLanguageExtensions {
	extension(ShaderLanguage self) {
		public string FileExtension =>
				self switch {
						ShaderLanguage.Glsl => "glsl",
						ShaderLanguage.Hlsl => "hlsl",
						ShaderLanguage.SpirV => "spv",
						_ => throw new ArgumentOutOfRangeException(nameof(self), self, null),
				};

		public SourceLanguage Shaderc_SourceLanguage =>
				self switch {
						ShaderLanguage.Glsl => SourceLanguage.Glsl,
						ShaderLanguage.Hlsl => SourceLanguage.Hlsl,
						ShaderLanguage.SpirV or _ => throw new ArgumentOutOfRangeException(nameof(self), self, null),
				};
	}
}