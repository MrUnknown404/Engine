namespace Engine3.Client.Graphics;

public static class ShaderLanguageExtensions {
	extension(ShaderLanguage self) {
		public string FileExtension =>
				self switch {
						ShaderLanguage.Glsl => "glsl",
						ShaderLanguage.Hlsl => "hlsl",
						ShaderLanguage.SpirV => "spv",
						_ => throw new ArgumentOutOfRangeException(nameof(self), self, null),
				};
	}
}