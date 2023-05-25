using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using USharpLibs.Common.Utils;

namespace USharpLibs.Engine.Client.GL.Models {
	[PublicAPI]
	public abstract class Model {
		protected BufferUsageHint BufferHint { get; }

		protected internal int VAO { get; set; }
		protected int VBO { get; set; }
		protected int EBO { get; set; }
		public bool WasSetup { get; protected set; }

		protected Model(BufferUsageHint bufferHint) => BufferHint = bufferHint;

		/// <summary> This method can be dangerous so should be avoided unless you know what you're doing! </summary>
		public void ForceSetupGL(bool doYouKnowWhatYouAreDoing) => ISetupGL();

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