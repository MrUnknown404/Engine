namespace Engine4.Client.Graphics;

public class NoGraphicsProvider : IGraphicsApiProvider {
	public GraphicsBuffer GetBuffer(ulong size) => throw new NotImplementedException();

	public void Cleanup() { }
}