namespace Engine4.Graphics;

public interface IGraphicsApiProvider {
	public GraphicsBuffer GetBuffer(ulong size);
}