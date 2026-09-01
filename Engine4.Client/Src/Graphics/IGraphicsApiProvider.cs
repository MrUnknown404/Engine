namespace Engine4.Client.Graphics;

public interface IGraphicsApiProvider {
	public GraphicsBuffer GetBuffer(ulong size);

	public void Cleanup();
}