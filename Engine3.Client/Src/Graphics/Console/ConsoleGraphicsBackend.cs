using System.Diagnostics;
using Engine3.Core;
using Engine3.Core.Utility;
using NLog;

namespace Engine3.Client.Graphics.Console;

public class ConsoleGraphicsBackend : GraphicsBackend {
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly Stopwatch renderStopwatch = new();

	public ushort Width { get; private set; }
	public ushort Height { get; private set; }

	private char[,] buffer; // TODO double buffer

	public ConsoleGraphicsBackend() : base(GraphicsApi.Console) {
		Width = (ushort)System.Console.BufferWidth;
		Height = (ushort)System.Console.BufferHeight;
		buffer = NewBuffer(Width, Height);
	}

	protected internal override void Setup(EngineGame game) { }

	public void SetCharAt(char value, ushort x, ushort y) => buffer[y, x] = value;

	public void Blit(char value, ushort x, ushort y, ushort w, ushort h) { // TODO support -w/h
		if (w == 0 || h == 0) { throw new ArgumentException("Width & Height cannot be zero"); }
		if (x + w > Width) { throw new ArgumentException("X + W cannot be greater than Buffer.Width"); }
		if (y + h > Height) { throw new ArgumentException("Y + H cannot be greater than Buffer.Height"); }

		for (int yi = 0; yi < h; yi++) {
			for (int xi = 0; xi < w; xi++) {
				buffer[y + yi, x + xi] = value; //
			}
		}
	}

	public bool TryResizeBuffer() {
		if (Width != System.Console.BufferWidth || Height != System.Console.BufferHeight) {
			checked {
				Width = (ushort)System.Console.BufferWidth;
				Height = (ushort)System.Console.BufferHeight;
			}

			buffer = NewBuffer(Width, Height);
			return true;
		}

		return false;
	}

	protected internal void UpdateBuffer(float time) { }

	protected internal void RenderBuffer() {
		renderStopwatch.Start();
		TimeSpan startTime = renderStopwatch.Elapsed;

		int width = Width * sizeof(char);

		char[] row = new char[Width];
		for (int y = 0; y < Height; y++) {
			Buffer.BlockCopy(buffer, y * width, row, 0, width);
			System.Console.SetCursorPosition(0, y);
			System.Console.Write(row);
		}

		renderStopwatch.Stop();
		TimeSpan endTime = renderStopwatch.Elapsed;
		double difference = (endTime - startTime).TotalMilliseconds;

		switch (difference) {
			case < 1: break;
			case < 10: Logger.Debug($"Setting console buffer took {difference:F}ms"); break;
			case < 100: Logger.Warn($"Setting console buffer took {difference:F}ms"); break;
			default: Logger.Error($"Setting console buffer took {difference:F}ms"); break;
		}
	}

	private static char[,] NewBuffer(ushort width, ushort height) {
		char[,] buffer = new char[height, width];

		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) { buffer[y, x] = ' '; }
		}

		return buffer;
	}

	protected internal override void Cleanup() { }
}