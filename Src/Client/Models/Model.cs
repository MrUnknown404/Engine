using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine2.Debug;

namespace USharpLibs.Engine2.Client.Models {
	[PublicAPI]
	public abstract class Model {
		public required BufferUsageHint BufferHint { protected get; init; }
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
			if (ModelErrorHandler.Assert(WasFreed, static () => new(ModelErrorHandler.Reason.WasFreed))) { return; }
			if (ModelErrorHandler.Assert(VAO != 0, static () => new(ModelErrorHandler.Reason.VAOIsFinal))) { return; }

			VAO = (uint)GL.GenVertexArray();
		}

		public void Free() {
			if (ModelErrorHandler.Assert(WasFreed, static () => new(ModelErrorHandler.Reason.WasFreed))) { return; }

			WasFreed = true;
			GL.DeleteVertexArray(VAO); // this should eventually lead to the vbo/ebo's being unbound. to my understanding
			VAO = 0;
		}

		public enum OnEmpty : byte {
			SilentlyFail = 0,
			Scream,
			Throw,
		}
	}
}