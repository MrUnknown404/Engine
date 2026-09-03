namespace Engine4.Client.Graphics.Console;

public class ConsoleGraphicsProvider : IGraphicsProvider {
	public void Blit(char[,] buffer, char value, int x, int y, uint width, uint height) {
		// this could probably be optimized?
		for (int yi = 0; yi < height; yi++) {
			for (int xi = 0; xi < width; xi++) {
				buffer[y + yi, x + xi] = value; //
			}
		}
	}

	public void Cleanup() { }
}