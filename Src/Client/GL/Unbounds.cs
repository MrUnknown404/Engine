using OpenTK.Windowing.Common;
using USharpLibs.Engine.Client.GL.Model;
using USharpLibs.Engine.Client.GL.OldMesh;

namespace USharpLibs.Engine.Client.GL {
	[Obsolete("Going to be removed.")]
	public sealed class UnboundIMesh<T> where T : IRawMesh {
		public readonly T IMesh;
		public UnboundIMesh(T mesh) => IMesh = mesh;
	}

	public sealed class UnboundModel<T> where T : RawModel {
		public T Model { get; }
		public UnboundModel(T model) => Model = model;
	}

	public interface IUnboundShader {
		internal void SetupGL();
		internal void OnResize(ResizeEventArgs args);
	}

	public sealed class UnboundShader<T> : IUnboundShader where T : RawShader {
		internal T Shader { get; }
		public UnboundShader(T shader) => Shader = shader;

		void IUnboundShader.SetupGL() => Shader.SetupGL();
		void IUnboundShader.OnResize(ResizeEventArgs args) => Shader.OnResize(args);
	}
}