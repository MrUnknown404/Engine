using System.Numerics;
using System.Runtime.InteropServices;
using Engine3.Exceptions;
using Engine3.Graphics.Vertex;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine3.Graphics.OpenGL {
	/*
	 * Note for code in this class. See https://github.com/opentk/opentk/issues/1409#issuecomment-1080789084
	 * or "With OpenTK 5 we are introducing types handles which means that instead of working with ints directly you will have something like ShaderHandle instead."
	 * Since the version of OpenTK i am currently using does not support this, these methods are wrappers are for that (with checks). these will probably be deleted once OpenTK adds support.
	 */

	// TODO perform extra checks when necessary below. check if handle is valid, handle is bound (if relevant), etc?

	[PublicAPI]
	public static class GLH {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static bool WereBindingsLoaded { get; private set; }

		public static ClearBufferMask ClearBufferMask { get; set; } = ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit;

		private static OpenGLContextHandle? openGLContextHandle;

		internal static void Setup(WindowHandle windowHandle) {
			Logger.Debug("Creating and setting OpenGL context...");
			openGLContextHandle = Toolkit.OpenGL.CreateFromWindow(windowHandle);
			Toolkit.OpenGL.SetCurrentContext(openGLContextHandle);

			Logger.Debug("Loading OpenGL bindings...");
			GLLoader.LoadBindings(Toolkit.OpenGL.GetBindingsContext(openGLContextHandle));
			WereBindingsLoaded = true;

			Logger.Debug($"- OpenGL version: {GL.GetString(StringName.Version)}");
			Logger.Debug($"- GLFW Version: {GLFW.GetVersionString()}");

#if DEBUG
			Logger.Debug("- OpenGL Callbacks are enabled");

			GL.Enable(EnableCap.DebugOutput);
			GL.Enable(EnableCap.DebugOutputSynchronous);

			uint[] defaultIds = [
					131185, // Nvidia static buffer notification
			];

			GL.DebugMessageControl(DebugSource.DebugSourceApi, DebugType.DebugTypeOther, DebugSeverity.DontCare, defaultIds.Length, defaultIds, false);

			GL.DebugMessageCallback(static (source, type, id, severity, length, message, _) => {
				switch (severity) {
					case DebugSeverity.DontCare: return;
					case DebugSeverity.DebugSeverityNotification:
						Logger.Debug($"OpenGL Notification: {id}. Source: {source.ToString()[11..]}, Type: {type.ToString()[9..]}");
						Logger.Debug($"- {Marshal.PtrToStringAnsi(message, length)}");
						break;
					case DebugSeverity.DebugSeverityHigh:
						Logger.Fatal($"OpenGL Fatal Error: {id}. Source: {source.ToString()[11..]}, Type: {type.ToString()[9..]}");
						Logger.Fatal($"- {Marshal.PtrToStringAnsi(message, length)}");
						break;
					case DebugSeverity.DebugSeverityMedium:
						Logger.Error($"OpenGL Error: {id}. Source: {source.ToString()[11..]}, Type: {type.ToString()[9..]}");
						Logger.Error($"- {Marshal.PtrToStringAnsi(message, length)}");
						break;
					case DebugSeverity.DebugSeverityLow:
						Logger.Warn($"OpenGL Warning: {id}. Source: {source.ToString()[11..]}, Type: {type.ToString()[9..]}");
						Logger.Warn($"- {Marshal.PtrToStringAnsi(message, length)}");
						break;
					default: throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
				}
			}, IntPtr.Zero);
#endif

			GL.ClearColor(0.1f, 0.1f, 0.1f, 1f);
			GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.CullFace);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
		}

		internal static void Render() {
			if (!WereBindingsLoaded || openGLContextHandle == null) { throw new EngineStateException(EngineStateException.Reason.NoGraphicsApi); }

			GL.Clear(ClearBufferMask);
			GameEngine.Render();
			Toolkit.OpenGL.SwapBuffers(openGLContextHandle);
		}

		public static void Bind(VertexArrayHandle handle) {
			if (!WereBindingsLoaded || openGLContextHandle == null) { throw new EngineStateException(EngineStateException.Reason.NoGraphicsApi); }

			// TODO store bound vao and check

			GL.BindVertexArray(handle.Handle);
		}

		public static void DrawElements(int drawSize, int offset = 0) {
			if (!WereBindingsLoaded || openGLContextHandle == null) { throw new EngineStateException(EngineStateException.Reason.NoGraphicsApi); }

			// TODO check if anything is bound?

			GL.DrawElements(PrimitiveType.Triangles, drawSize, DrawElementsType.UnsignedInt, offset);
		}

		[MustUseReturnValue]
		public static BufferHandle CreateBuffer() {
			if (!WereBindingsLoaded || openGLContextHandle == null) { throw new EngineStateException(EngineStateException.Reason.NoGraphicsApi); }

			return new(GL.CreateBuffer());
		}

		public static void DeleteBuffer(BufferHandle handle) {
			if (!WereBindingsLoaded || openGLContextHandle == null) { throw new EngineStateException(EngineStateException.Reason.NoGraphicsApi); }

			GL.DeleteBuffer(handle.Handle);
		}

		[MustUseReturnValue]
		public static BufferHandle[] CreateBuffers(int count) {
			if (!WereBindingsLoaded || openGLContextHandle == null) { throw new EngineStateException(EngineStateException.Reason.NoGraphicsApi); }

			int[] handles = new int[count];
			GL.CreateBuffers(handles.Length, handles);
			return handles.Select(static h => new BufferHandle(h)).ToArray();
		}

		public static void DeleteBuffers(BufferHandle[] handles) {
			if (!WereBindingsLoaded || openGLContextHandle == null) { throw new EngineStateException(EngineStateException.Reason.NoGraphicsApi); }

			GL.DeleteBuffers(handles.Length, handles.Select(static h => h.Handle).ToArray());
		}

		[MustUseReturnValue]
		public static VertexArrayHandle CreateVertexArray() {
			if (!WereBindingsLoaded || openGLContextHandle == null) { throw new EngineStateException(EngineStateException.Reason.NoGraphicsApi); }

			return new(GL.CreateVertexArray());
		}

		public static void DeleteVertexArray(VertexArrayHandle handle) {
			if (!WereBindingsLoaded || openGLContextHandle == null) { throw new EngineStateException(EngineStateException.Reason.NoGraphicsApi); }

			GL.DeleteVertexArray(handle.Handle);
		}

		public static void NamedBufferData<T>(BufferHandle handle, ReadOnlySpan<T> data, VertexBufferObjectUsage usage) where T : unmanaged, INumber<T> {
			if (!WereBindingsLoaded || openGLContextHandle == null) { throw new EngineStateException(EngineStateException.Reason.NoGraphicsApi); }

			int size;
			unsafe { size = data.Length * sizeof(T); }

			GL.NamedBufferData(handle.Handle, size, data, usage);
		}

		public static void NamedBufferData(BufferHandle handle, int size, VertexBufferObjectUsage usage) {
			if (!WereBindingsLoaded || openGLContextHandle == null) { throw new EngineStateException(EngineStateException.Reason.NoGraphicsApi); }

			GL.NamedBufferData(handle.Handle, size, IntPtr.Zero, usage);
		}

		public static void NamedBufferStorage<T>(BufferHandle handle, ReadOnlySpan<T> data, BufferStorageMask mask) where T : unmanaged, INumber<T> {
			if (!WereBindingsLoaded || openGLContextHandle == null) { throw new EngineStateException(EngineStateException.Reason.NoGraphicsApi); }

			int size;
			unsafe { size = data.Length * sizeof(T); }

			GL.NamedBufferStorage(handle.Handle, size, data, mask);
		}

		public static void NamedBufferStorage(BufferHandle handle, int size, BufferStorageMask mask) {
			if (!WereBindingsLoaded || openGLContextHandle == null) { throw new EngineStateException(EngineStateException.Reason.NoGraphicsApi); }

			GL.NamedBufferStorage(handle.Handle, size, IntPtr.Zero, mask);
		}

		public static void NamedBufferSubData<T>(BufferHandle handle, ReadOnlySpan<T> data, int offset) where T : unmanaged, INumber<T> {
			if (!WereBindingsLoaded || openGLContextHandle == null) { throw new EngineStateException(EngineStateException.Reason.NoGraphicsApi); }

			int size;
			unsafe { size = data.Length * sizeof(T); }

			GL.NamedBufferSubData(handle.Handle, offset, size, data);
		}

		public static void VertexArrayVertexBuffer(GLVertexArray vertexArray, BufferHandle bufferHandle, uint bindingIndex = 0, int offset = 0) {
			if (!WereBindingsLoaded || openGLContextHandle == null) { throw new EngineStateException(EngineStateException.Reason.NoGraphicsApi); }

			GL.VertexArrayVertexBuffer(vertexArray.Handle.Handle, bindingIndex, bufferHandle.Handle, offset, vertexArray.VertexFormatSize);
		}

		public static void VertexArrayElementBuffer(VertexArrayHandle vaoHandle, BufferHandle bufferHandle) {
			if (!WereBindingsLoaded || openGLContextHandle == null) { throw new EngineStateException(EngineStateException.Reason.NoGraphicsApi); }

			GL.VertexArrayElementBuffer(vaoHandle.Handle, bufferHandle.Handle);
		}

		public static void EnableVertexArrayAttrib(VertexArrayHandle handle, uint index) {
			if (!WereBindingsLoaded || openGLContextHandle == null) { throw new EngineStateException(EngineStateException.Reason.NoGraphicsApi); }

			GL.EnableVertexArrayAttrib(handle.Handle, index);
		}

		public static void VertexArrayAttribFormat(VertexArrayHandle handle, uint attribIndex, VertexAttributeFormat vertexAttrib, uint offset, bool normalized = false) {
			if (!WereBindingsLoaded || openGLContextHandle == null) { throw new EngineStateException(EngineStateException.Reason.NoGraphicsApi); }

			GL.VertexArrayAttribFormat(handle.Handle, attribIndex, vertexAttrib.ComponentCount, vertexAttrib.AttribType, normalized, offset);
		}

		public static void VertexArrayAttribBinding(VertexArrayHandle handle, uint attribIndex, uint bindingIndex = 0) {
			if (!WereBindingsLoaded || openGLContextHandle == null) { throw new EngineStateException(EngineStateException.Reason.NoGraphicsApi); }

			GL.VertexArrayAttribBinding(handle.Handle, attribIndex, bindingIndex);
		}
	}
}