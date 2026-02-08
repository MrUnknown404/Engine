using ImGuiNET;
using NLog;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Client.Graphics.OpenGL {
	public abstract class OpenGLRenderer : Renderer<OpenGLWindow, OpenGLGraphicsBackend, OpenGLImGuiBackend>, IGraphicsResourceProvider {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		protected OpenGLResourceProvider ResourceProvider { get; } = new();
		protected VertexArrayHandle? EmptyVao { get; private set; }

		public ClearBufferMask ClearBufferMask { get; set; } = ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit;

		protected OpenGLRenderer(OpenGLGraphicsBackend graphicsBackend, OpenGLWindow window, OpenGLImGuiBackend? imGuiBackend = null) : base(graphicsBackend, window, imGuiBackend) { }

		public override void Setup() {
			Window.MakeContextCurrent();

			ImGuiBackend?.Setup();

			GL.Enable(EnableCap.CullFace);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.ClearColor(Window.ClearColor);

			Toolkit.OpenGL.SetSwapInterval(GraphicsBackend.SwapInterval);

			EmptyVao = new(GL.CreateVertexArray());
			GL.BindVertexArray(EmptyVao.Value.Handle); // Some hardware requires vao to be bound even if it's not in use
			Logger.Debug($"EmptyVao has ShaderHandle: {EmptyVao.Value.Handle}");
		}

		protected internal override void Render(float delta) {
			Window.MakeContextCurrent();

			ResourceProvider.TryCleanup(); // TODO don't destroy every frame?

			GL.ClearColor(Window.ClearColor);
			GL.Clear(ClearBufferMask);

			if (Window.WasResized) {
				Toolkit.Window.GetFramebufferSize(Window.WindowHandle, out Vector2i frameBufferSize);
				GL.Viewport(0, 0, frameBufferSize.X, frameBufferSize.Y);
				Window.WasResized = false;
			}

			ImDrawDataPtr imDrawData = null;
			bool shouldDrawImGui = false;

			if (ImGuiBackend != null) {
				shouldDrawImGui = ImGuiBackend.NewFrame(out imDrawData);
				if (shouldDrawImGui) { ImGuiBackend.UpdateBuffers(imDrawData); }
			}

			DrawFrame(delta); // TODO do i want to store/restore state? // what if i made a gl state object and stored each pipeline's state?

			if (ImGuiBackend != null && shouldDrawImGui) { ImGuiBackend.DrawFrame(imDrawData); }

			Toolkit.OpenGL.SwapBuffers(Window.GLContextHandle);
		}

		protected abstract void DrawFrame(float delta);

		protected override void PrepareCleanup() => Window.MakeContextCurrent();
		protected override void Cleanup() => ResourceProvider.CleanupAll();
	}
}