using Engine3.Graphics.Vertex;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics;

namespace Engine3.Graphics.OpenGL {
	[PublicAPI]
	public sealed class GLVertexArray {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public List<GLBuffer> Buffers { get; } = new();
		public VertexAttributeFormat[] VertexFormat { get; }
		public byte VertexFormatSize { get; }

		public VertexArrayHandle Handle { get; private set; }
		public bool WasFreed { get; private set; }
		public bool WereAttributesBound { get; private set; }

		public bool HasHandle => Handle.Handle != 0;

		public GLVertexArray(VertexAttributeFormat[] vertexFormat) {
			VertexFormat = vertexFormat;
			VertexFormatSize = (byte)vertexFormat.Sum(static f => f.ComponentCount * f.AttribByteSize);
		}

		public void GenHandle() {
			if (WasFreed) { throw new Exception(); } // TODO handle/exception
			if (HasHandle) { throw new Exception(); } // TODO handle/exception

			Handle = GLH.CreateVertexArray();
		}

		public void AddVertexBuffer(GLBuffer buffer, uint bindingIndex = 0, int offset = 0) {
			if (WasFreed) { throw new Exception(); } // TODO handle/exception
			if (!HasHandle) { throw new Exception(); } // TODO handle/exception

			// TODO check if buffer is already in vao

			Buffers.Add(buffer);
			GLH.VertexArrayVertexBuffer(this, buffer.Handle, bindingIndex, offset);
		}

		public void AddIndexBuffer(GLBuffer buffer) {
			if (WasFreed) { throw new Exception(); } // TODO handle/exception
			if (!HasHandle) { throw new Exception(); } // TODO handle/exception

			// TODO check if buffer is already in vao?

			Buffers.Add(buffer);
			GLH.VertexArrayElementBuffer(Handle, buffer.Handle);
		}

		public void BindVertexAttributes() { // TODO this may require rethinking. i think this won't work when i setup multiple VBOs. should this be moved into GLBuffer? or should it be in VAO???? send help
			if (WasFreed) { throw new Exception(); } // TODO handle/exception
			if (!HasHandle) { throw new Exception(); } // TODO handle/exception

			uint offset = 0;

			for (uint i = 0; i < VertexFormat.Length; i++) {
				VertexAttributeFormat attributeFormat = VertexFormat[i];
				GLH.EnableVertexArrayAttrib(Handle, i);
				GLH.VertexArrayAttribFormat(Handle, i, attributeFormat, offset);
				GLH.VertexArrayAttribBinding(Handle, i);
				offset += VertexFormatSize;
			}

			WereAttributesBound = true;
		}

		public void Free() {
			if (WasFreed) {
				Logger.Warn($"Attempted to free an already freed {nameof(GLVertexArray)}");
				return;
			} else if (!HasHandle) {
				Logger.Warn($"Attempted to free a {nameof(GLVertexArray)} with no handle");
				return;
			}

			GLH.DeleteVertexArray(Handle);
			GLH.DeleteBuffers(Buffers.Select(static b => b.Handle).ToArray());

			foreach (GLBuffer buffer in Buffers) { buffer.Free(false); } // OpenGL buffers deleted above
			Buffers.Clear();

			Handle = new();
			WasFreed = true;
		}
	}
}