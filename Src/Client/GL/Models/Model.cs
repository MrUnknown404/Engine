using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using USharpLibs.Common.Utils;

namespace USharpLibs.Engine.Client.GL.Models {
	[PublicAPI]
	public abstract class Model {
		protected BufferUsageHint BufferHint { get; }

		public int VAO { get; protected set; } // TODO remove public access. protected internal
		protected int VBO { get; set; }
		protected int EBO { get; set; }
		public bool WasSetup { get; protected set; }

		protected Model(BufferUsageHint bufferHint) => BufferHint = bufferHint;

		public void SetupGL() {
			if (GameEngine.LoadState < LoadState.SetupGL) { throw new Exception("Cannot setup a model's GL code too early!"); }
			ISetupGL();
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
}