using System.Diagnostics.CodeAnalysis;

namespace Engine3.Graphics {
	public enum ShaderLanguage {
		Glsl,
		Hlsl,
		SpirV,
	}

	public static class ShaderLanguageExtensions {
		extension(ShaderLanguage self) {
			[SuppressMessage("Performance", "CA1822:Mark members as static")]
			public string AssetFolderName =>
					self switch {
							ShaderLanguage.Glsl => "GLSL",
							ShaderLanguage.Hlsl => "HLSL",
							ShaderLanguage.SpirV => "SPIR-V",
							_ => throw new ArgumentOutOfRangeException(nameof(self), self, null),
					};
		}
	}
}