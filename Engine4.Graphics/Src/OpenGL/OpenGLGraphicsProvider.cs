namespace Engine4.Graphics.OpenGL;

public class OpenGLGraphicsProvider : IGraphicsApiProvider {
	internal OpenGLGraphicsProvider() { }

	public GraphicsBuffer GetBuffer(ulong size) => throw new NotImplementedException(); // TODO
}