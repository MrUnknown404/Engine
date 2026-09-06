using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine4.Client.Rendering;
using JetBrains.Annotations;

namespace Engine4.Client.Graphics.Console;

public sealed class ConsoleRenderer : Renderer {
	// TODO this'll need to disable regular console output

	public char[,] Buffer { get; private set; } // TODO double buffer?
	public ushort Width { get; private set; }
	public ushort Height { get; private set; }

	private readonly ConsoleRenderTarget renderTarget;

	internal ConsoleRenderer(ConsoleRenderTarget renderTarget, IGraphicsProvider graphicsProvider, params RenderPass[] renderPasses) : base(renderTarget, graphicsProvider, renderPasses) {
		this.renderTarget = renderTarget;
		Width = (ushort)System.Console.BufferWidth; // TODO these may update!
		Height = (ushort)System.Console.BufferHeight;
		Buffer = CreateBuffer(Width, Height);

		System.Console.CursorVisible = false; // TODO place elsewhere?
	}

	protected override void Render(float delta) {
		if (!TryResizeBuffer()) { ClearBuffer(Buffer, ' '); } // if buffer was not reset. clear buffer

		DrawFrame();
		PresentFrame();
	}

	/// <returns> true if buffer was resized </returns>
	private bool TryResizeBuffer() {
		ushort width, height;
		checked {
			width = (ushort)System.Console.BufferWidth;
			height = (ushort)System.Console.BufferHeight;
		}

		if (Width != width || Height != height) {
			Width = width;
			Height = height;
			Buffer = CreateBuffer(Width, Height);
			return true;
		}

		return false;
	}

	private void DrawFrame() {
		foreach (RenderPass renderPass in RenderPasses) {
			renderPass.RecordCommandBuffer(); //
		}
	}

	private void PresentFrame() {
		int width = Width * sizeof(char);

		char[] row = new char[Width];
		for (int y = 0; y < Height; y++) {
			System.Buffer.BlockCopy(Buffer, y * width, row, 0, width);
			System.Console.SetCursorPosition(0, y);
			System.Console.Write(row);
		}
	}

	[MustUseReturnValue]
	private static char[,] CreateBuffer(ushort width, ushort height) {
		char[,] buffer = new char[height, width];
		ClearBuffer(buffer, ' ');
		return buffer;
	}

	private static void ClearBuffer(char[,] buffer, char value) {
		ref byte reference = ref MemoryMarshal.GetArrayDataReference(buffer);
		Span<char> span = MemoryMarshal.CreateSpan(ref Unsafe.As<byte, char>(ref reference), buffer.Length);
		span.Fill(value);
	}
}