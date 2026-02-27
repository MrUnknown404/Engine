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

		protected OpenGLRenderer(OpenGLGraphicsBackend graphicsBackend, OpenGLWindow window) : base(graphicsBackend, window) { }

		protected internal override void Setup() {
			Window.MakeContextCurrent();

			ImGuiBackend?.Setup();

			GL.Enable(EnableCap.CullFace);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.ClearColor(Window.ClearColor);

			Toolkit.OpenGL.SetSwapInterval(GraphicsBackend.Settings.SwapInterval);

			EmptyVao = new(GL.CreateVertexArray());
			GL.BindVertexArray(EmptyVao.Value.Handle); // Some hardware requires vao to be bound even if it's not in use
			Logger.Debug($"EmptyVao has ShaderHandle: {EmptyVao.Value.Handle}");
		}

		protected override void PrepareRender() => Window.MakeContextCurrent(); // for now this can go here since it's called first
		protected override void TryCleanupResources() => ResourceProvider.TryCleanupResources();
		protected override bool TryNextFrame() => true;

		protected override void BeginFrame() {
			GL.ClearColor(Window.ClearColor);
			GL.Clear(ClearBufferMask);

			if (Window.WasResized) {
				Vector2i frameBufferSize = Window.GetFrameBufferSize();
				GL.Viewport(0, 0, frameBufferSize.X, frameBufferSize.Y);
				Window.WasResized = false;
			}

			// TODO do i want to store/restore state? // what if i made a gl state object and stored each pipeline's state?
		}

		protected override void DrawImGuiFrame(ImDrawDataPtr imDrawData) => ImGuiBackend!.DrawFrame(imDrawData); // shouldn't be null if this is called

		protected override void EndFrame() => Toolkit.OpenGL.SwapBuffers(Window.GLContextHandle);

		protected override void PrepareCleanup() => Window.MakeContextCurrent();
		protected override void Cleanup() => ResourceProvider.CleanupAll();
	}
}