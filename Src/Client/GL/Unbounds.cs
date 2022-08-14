using OpenTK.Windowing.Common;
using USharpLibs.Engine.Client.GL.Mesh;

namespace USharpLibs.Engine.Client.GL {
	public struct UnboundIMesh<T> where T : RawMesh {
		public readonly T IMesh;
		public UnboundIMesh(T mesh) => IMesh = mesh;
	}

	public interface IUnboundShader {
		internal void SetupGL();
		internal void OnResize(ResizeEventArgs args);
	}

	public readonly struct UnboundShader<T> : IUnboundShader where T : RawShader {
		internal readonly T Shader;
		public UnboundShader(T shader) => Shader = shader;

		void IUnboundShader.SetupGL() => Shader.SetupGL();
		void IUnboundShader.OnResize(ResizeEventArgs args) => Shader.OnResize(args);
	}
}