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
		}
	}
}