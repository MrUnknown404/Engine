using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using USharpLibs.Common.Utils;

namespace USharpLibs.Engine.Client.GL.Models {
	public abstract class Model {
		protected BufferUsageHint BufferHint { get; }

		public int VAO { get; protected set; } // TODO remove public access. protected internal
		protected int VBO { get; set; }
		protected int EBO { get; set; }
		public bool WasSetup { get; protected set; }

		internal Model(BufferUsageHint bufferHint) => BufferHint = bufferHint;

		public void SetupGL() {
			if (GameEngine.LoadState < LoadState.SetupGL) { throw new Exception("Cannot setup a model's GL code too early!"); }
			ISetupGL();
			WasSetup = true;
		}

		public void Draw() {
			if (!WasSetup) {
				Logger.Warn("Model was not setup!");
				return;
			} else if (GLH.CurrentVAO != VAO) {
				Logger.Warn("Model is not bound!");
				return;
			}

			IDraw();
		}

		protected abstract void ISetupGL();
		protected abstract void IDraw();
	}

	[PublicAPI]
	public abstract class Model<T> : Model where T : Model {
		protected Model(BufferUsageHint bufferHint) : base(bufferHint) { }

		public abstract T SetMesh(Mesh mesh, params Mesh[] meshes);
		public abstract T SetMesh(List<Mesh> meshes);
	}
}