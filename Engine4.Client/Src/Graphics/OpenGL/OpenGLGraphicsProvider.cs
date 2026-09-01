namespace Engine4.Client.Graphics.OpenGL;

public class OpenGLGraphicsProvider : IGraphicsApiProvider {
	internal OpenGLGraphicsProvider() { }

	public GraphicsBuffer GetBuffer(ulong size) => throw new NotImplementedException(); // TODO

	public void Cleanup() { } // TODO cleanup
}