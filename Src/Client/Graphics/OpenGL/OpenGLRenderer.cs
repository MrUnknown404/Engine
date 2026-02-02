using Engine3.Client.Graphics.OpenGL.Objects;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Client.Graphics.OpenGL {
	public abstract class OpenGLRenderer : Renderer<OpenGLWindow, OpenGLGraphicsBackend> {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		protected VertexArrayHandle? EmptyVao { get; private set; }

		public ClearBufferMask ClearBufferMask { get; set; } = ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit;

		private readonly NamedResourceManager<ProgramPipeline> programPipelineManager = new();
		private readonly NamedResourceManager<OpenGLBuffer> bufferManager = new();
		private readonly NamedResourceManager<OpenGLImage> imageManager = new();

		protected OpenGLRenderer(OpenGLGraphicsBackend graphicsBackend, OpenGLWindow window) : base(graphicsBackend, window) { }

		public override void Setup() {
			Window.MakeContextCurrent();

			GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.CullFace);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.ClearColor(Window.ClearColor);

			Toolkit.OpenGL.SetSwapInterval(GraphicsBackend.SwapInterval);

			EmptyVao = new(GL.CreateVertexArray());
			GL.BindVertexArray(EmptyVao.Value.Handle); // Some hardware requires vao to be bound even if it's not in use
			Logger.Debug($"EmptyVao has Handle: {EmptyVao.Value.Handle}");
		}

		protected internal override void Render(float delta) {
			Window.MakeContextCurrent();

			GL.ClearColor(Window.ClearColor);
			GL.Clear(ClearBufferMask);

			if (Window.WasResized) {
				Toolkit.Window.GetFramebufferSize(Window.WindowHandle, out Vector2i framebufferSize);
				GL.Viewport(0, 0, framebufferSize.X, framebufferSize.Y);
				Window.WasResized = false;
			}

			DrawFrame(delta);

			Toolkit.OpenGL.SwapBuffers(Window.GLContextHandle);
		}

		protected abstract void DrawFrame(float delta);

		[MustUseReturnValue]
		protected ProgramPipeline CreateProgramPipeline(string debugName, OpenGLShader? vert, OpenGLShader? frag, OpenGLShader? geom = null, OpenGLShader? tessEval = null, OpenGLShader? tessCtrl = null) {
			ProgramPipeline programPipeline = new(debugName, vert, frag, geom, tessEval, tessCtrl);
			programPipelineManager.Add(programPipeline);
			return programPipeline;
		}

		[MustUseReturnValue]
		protected OpenGLBuffer CreateBuffer(string debugName, BufferStorageMask storageMask, ulong bufferSize) {
			OpenGLBuffer buffer = new(debugName, bufferSize, storageMask);
			bufferManager.Add(buffer);
			return buffer;
		}

		[MustUseReturnValue]
		protected OpenGLImage CreateImage(string debugName, TextureMinFilter minFilter = TextureMinFilter.Linear, TextureMagFilter magFilter = TextureMagFilter.Linear, TextureWrapMode wrapModeU = TextureWrapMode.Repeat,
			TextureWrapMode wrapModeV = TextureWrapMode.Repeat) {
			OpenGLImage image = new(debugName, minFilter, magFilter, wrapModeU, wrapModeV);
			imageManager.Add(image);
			return image;
		}

		protected internal void DestroyResource(ProgramPipeline programPipeline) => programPipelineManager.Destroy(programPipeline);
		protected internal void DestroyResource(OpenGLBuffer buffer) => bufferManager.Destroy(buffer);

		protected override void PrepareCleanup() => Window.MakeContextCurrent();

		protected override void Cleanup() {
			bufferManager.CleanupAll();
			imageManager.CleanupAll();

			programPipelineManager.CleanupAll();
		}
	}
}