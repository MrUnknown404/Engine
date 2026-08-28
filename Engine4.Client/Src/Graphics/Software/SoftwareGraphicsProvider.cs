namespace Engine4.Client.Graphics.Software;

public class SoftwareGraphicsProvider : IGraphicsApiProvider {
	internal SoftwareGraphicsProvider() { }

	public GraphicsBuffer GetBuffer(ulong size) => throw new NotImplementedException(); // TODO
}