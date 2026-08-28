namespace Engine4.Client.Graphics;

public readonly record struct TestVertex : IVertex {
	public readonly float X;
	public readonly float Y;
	public readonly float Z;

	public TestVertex(float x, float y, float z) {
		X = x;
		Y = y;
		Z = z;
	}
}