using Engine3.Client.Client.Graphics.DataStructs;

namespace Engine3.Client.Client.Graphics;

public class RenderMesh {
	public byte[] Vertices { get; }
	public uint[] Indices { get; }
	public Material? Material { get; init; }

	public RenderMesh(byte[] vertices, uint[] indices) {
		Vertices = vertices;
		Indices = indices;
	}
}