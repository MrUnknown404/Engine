namespace Engine3.Graphics {
	public enum ShaderLanguage {
		Glsl,
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
		}
	}
}