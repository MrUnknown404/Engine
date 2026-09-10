using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine4.IO;
using JetBrains.Annotations;

namespace Engine4.Client.Graphics;

public abstract class ConsoleRenderer {
	public char[,] Buffer { get; private set; } // TODO double buffer?
	public ushort Width { get; private set; }
	public ushort Height { get; private set; }

	public ulong FrameCount { get; private set; }

	internal bool WasSetup { get; private set; }
	private bool wasDestroyed;

	private readonly object? vulkanRenderer; // TODO how this gonna work. we should somehow steal the final result and do some post processing then output to console

	protected ConsoleRenderer(object? vulkanRenderer = null) {
		this.vulkanRenderer = vulkanRenderer;
		TryResize();
		Buffer = CreateBuffer(Width, Height);
	}

	internal void InternalSetup() {
		if (WasSetup || wasDestroyed) { return; }

		Console.CursorVisible = false;
		LoggerH.PauseConsoleLogging();

		Setup();
		WasSetup = true;
	}

	internal void InternalRender(float delta) {
		if (!WasSetup || wasDestroyed) { throw new Exception(); } // TODO exception

		ClearBuffer(Buffer, ' ');

		if (vulkanRenderer == null) {
			DrawFrame(delta); //
		} else {
			// TODO impl vulkan rendering to console
		}

		if (TryResize()) { Buffer = CreateBuffer(Width, Height); }
		PresentFrame();
		FrameCount++;
	}

	internal void Cleanup() {
		LoggerH.UnpauseConsoleLogging();
		wasDestroyed = true;
	}

	protected abstract void Setup();
	protected abstract void DrawFrame(float delta);

	private bool TryResize() {
		ushort oldWidth = Width;
		ushort oldHeight = Height;

		checked { // throw if out of bounds
			Width = (ushort)Console.BufferWidth;
			Height = (ushort)Console.BufferHeight;
		}

		return oldWidth != Width || oldHeight != Height;
	}

	private void PresentFrame() {
		int width = Width * sizeof(char);

		char[] row = new char[Width];
		for (int y = 0; y < Height; y++) {
			System.Buffer.BlockCopy(Buffer, y * width, row, 0, width);
			Console.SetCursorPosition(0, y);
			Console.Write(row);
		}
	}

	protected void Blit(char value, int x, int y, uint width, uint height) {
		// TODO check overflow
		// this could probably be optimized?
		for (int yi = 0; yi < height; yi++) {
			for (int xi = 0; xi < width; xi++) {
				Buffer[y + yi, x + xi] = value; //
			}
		}
	}

	[MustUseReturnValue]
	protected static char[,] CreateBuffer(ushort width, ushort height) {
		char[,] buffer = new char[height, width];
		ClearBuffer(buffer, ' ');
		return buffer;
	}

	protected static void ClearBuffer(char[,] buffer, char value) {
		ref byte reference = ref MemoryMarshal.GetArrayDataReference(buffer);
		Span<char> span = MemoryMarshal.CreateSpan(ref Unsafe.As<byte, char>(ref reference), buffer.Length);
		span.Fill(value);
	}
}