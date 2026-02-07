using System.Reflection;
using Engine3.Client.Graphics.OpenGL.Objects;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Client.Graphics.OpenGL {
	public abstract class OpenGLRenderer : Renderer<OpenGLWindow, OpenGLGraphicsBackend, OpenGLImGuiBackend> {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		protected VertexArrayHandle? EmptyVao { get; private set; }

		public ClearBufferMask ClearBufferMask { get; set; } = ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit;

		private readonly ResourceManager<ProgramPipeline> programPipelineManager = new(); // this is per context i think
		private readonly ResourceManager<OpenGLShader> shaderManager = new(); // i think these are shared? etc
		private readonly ResourceManager<OpenGLBuffer> bufferManager = new();
		private readonly ResourceManager<OpenGLImage> imageManager = new();

		protected OpenGLRenderer(OpenGLGraphicsBackend graphicsBackend, OpenGLWindow window, OpenGLImGuiBackend? imGuiBackend = null) : base(graphicsBackend, window, imGuiBackend) { }

		public override void Setup() {
			Window.MakeContextCurrent();

			ImGuiBackend?.Setup();

			GL.Enable(EnableCap.DepthTest);
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

			programPipelineManager.TryCleanup();
			shaderManager.TryCleanup();
			bufferManager.TryCleanup();
			imageManager.TryCleanup();

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
		protected OpenGLShader CreateShader(string debugName, string fileLocation, ShaderType shaderType, Assembly assembly) {
			OpenGLShader shader = new(debugName, fileLocation, shaderType, assembly);
			shaderManager.Add(shader);
			return shader;
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

		protected void DestroyResource(ProgramPipeline programPipeline) => programPipelineManager.EnqueueDestroy(programPipeline);
		protected void DestroyResource(OpenGLShader shader) => shaderManager.EnqueueDestroy(shader);
		protected void DestroyResource(OpenGLBuffer buffer) => bufferManager.EnqueueDestroy(buffer);
		protected void DestroyResource(OpenGLImage image) => imageManager.EnqueueDestroy(image);

		protected override void PrepareCleanup() => Window.MakeContextCurrent();

		protected override void Cleanup() {
			bufferManager.CleanupAll();
			imageManager.CleanupAll();

			programPipelineManager.CleanupAll();
		}
	}
}