using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine2.Exceptions;

namespace USharpLibs.Engine2.Client.Models {
	[PublicAPI]
	public abstract class Model {
		public BufferUsageHint BufferHint { protected get; init; } = BufferUsageHint.StaticDraw;
		public OnEmpty IfBuildEmpty { internal get; init; } = OnEmpty.Scream;
		public OnEmpty IfDrawEmpty { internal get; init; } = OnEmpty.Scream;

		protected internal uint VAO { get; private set; }
		protected internal bool WasFreed { get; private set; }

		protected internal abstract bool IsBuildMeshEmpty { get; }
		protected internal abstract bool IsDrawDataEmpty { get; }

		internal Model() { }

		protected internal abstract void Build();
		protected internal abstract void Draw();

		protected internal abstract bool CanBuild();

		public void GenerateVAO() {
			if (WasFreed) { throw new ModelException(ModelException.Reason.WasFreed); }
			if (VAO != 0) { throw new ModelException(ModelException.Reason.VAOIsFinal); }

			VAO = (uint)GL.GenVertexArray();
		}

		public virtual void Free() {
			if (WasFreed) { throw new ModelException(ModelException.Reason.ModelAlreadyFreed); }

			WasFreed = true;
			GL.DeleteVertexArray(VAO);
			VAO = 0;
		}

		public enum OnEmpty : byte {
			SilentlyFail = 0,
			Scream,
			Throw,
		}
	}
}