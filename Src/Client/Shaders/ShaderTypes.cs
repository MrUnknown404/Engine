namespace USharpLibs.Engine2.Client.Shaders {
	[Flags]
	public enum ShaderTypes : byte {
		Vertex = 1 << 0,
		TesselationControl = 1 << 1,
		TesselationEvaluation = 1 << 2,
		Geometry = 1 << 3,
		Fragment = 1 << 4,
		//Compute = 1 << 5, // TODO implement
	}
}