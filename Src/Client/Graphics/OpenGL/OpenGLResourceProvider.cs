using System.Reflection;
using Engine3.Client.Graphics.OpenGL.Objects;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.OpenGL;

namespace Engine3.Client.Graphics.OpenGL {
	public class OpenGLResourceProvider : IGraphicsResourceProvider {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private readonly ResourceManager<ProgramPipeline> programPipelineManager = new();
		private readonly ResourceManager<OpenGLShader> shaderManager = new();
		private readonly ResourceManager<OpenGLBuffer> bufferManager = new();
		private readonly ResourceManager<OpenGLImage> imageManager = new();

		[MustUseReturnValue]
		public ProgramPipeline CreateProgramPipeline(string debugName, OpenGLShader? vert, OpenGLShader? frag, OpenGLShader? geom = null, OpenGLShader? tessEval = null, OpenGLShader? tessCtrl = null) {
			ProgramPipeline programPipeline = new(debugName, vert, frag, geom, tessEval, tessCtrl);
			programPipelineManager.Add(programPipeline);
			return programPipeline;
		}

		[MustUseReturnValue]
		public OpenGLShader CreateShader(string debugName, string fileLocation, ShaderType shaderType, Assembly assembly) {
			OpenGLShader shader = new(debugName, fileLocation, shaderType, assembly);
			shaderManager.Add(shader);
			return shader;
		}

		[MustUseReturnValue]
		public OpenGLBuffer CreateBuffer(string debugName, BufferStorageMask storageMask, ulong bufferSize) {
			OpenGLBuffer buffer = new(debugName, bufferSize, storageMask);
			bufferManager.Add(buffer);
			return buffer;
		}

		[MustUseReturnValue]
		public OpenGLImage CreateImage(string debugName, TextureMinFilter minFilter = TextureMinFilter.Linear, TextureMagFilter magFilter = TextureMagFilter.Linear, TextureWrapMode wrapModeU = TextureWrapMode.Repeat,
			TextureWrapMode wrapModeV = TextureWrapMode.Repeat) {
			OpenGLImage image = new(debugName, minFilter, magFilter, wrapModeU, wrapModeV);
			imageManager.Add(image);
			return image;
		}

		public void EnqueueDestroy(ProgramPipeline programPipeline) {
			Logger.Trace($"Requesting to destroy {nameof(ProgramPipeline)} ({programPipeline.ProgramPipelineHandle.Handle:X16})");
			programPipelineManager.EnqueueDestroy(programPipeline);
		}

		public void EnqueueDestroy(OpenGLShader shader) {
			Logger.Trace($"Requesting to destroy {nameof(OpenGLShader)} ({shader.ShaderHandle.Handle:X16})");
			shaderManager.EnqueueDestroy(shader);
		}

		public void EnqueueDestroy(OpenGLBuffer buffer) {
			Logger.Trace($"Requesting to destroy {nameof(OpenGLBuffer)} ({buffer.BufferHandle.Handle:X16})");
			bufferManager.EnqueueDestroy(buffer);
		}

		public void EnqueueDestroy(OpenGLImage image) {
			Logger.Trace($"Requesting to destroy {nameof(OpenGLImage)} ({image.TextureHandle.Handle:X16})");
			imageManager.EnqueueDestroy(image);
		}

		public void TryCleanup() {
			programPipelineManager.TryCleanup();
			shaderManager.TryCleanup();
			bufferManager.TryCleanup();
			imageManager.TryCleanup();
		}

		public void CleanupAll() {
			programPipelineManager.CleanupAll();
			shaderManager.CleanupAll();
			bufferManager.CleanupAll();
			imageManager.CleanupAll();
		}
	}
}