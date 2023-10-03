using OpenTK.Windowing.Common;
using USharpLibs.Engine.Client.GL.Models;

namespace USharpLibs.Engine.Client.GL {
	public sealed class UnboundModel<T> where T : Model {
		public T Model { get; }
		public UnboundModel(T model) => Model = model;
		public void SetupGL() => Model.SetupGL();
	}

	public interface IUnboundShader {
		internal void SetupGL();
		internal void OnResize(ResizeEventArgs args);
	}

	public sealed class UnboundShader<T> : IUnboundShader where T : Shader {
		internal T Shader { get; }
		public UnboundShader(T shader) => Shader = shader;

		void IUnboundShader.SetupGL() => Shader.SetupGL();
		void IUnboundShader.OnResize(ResizeEventArgs args) => Shader.OnResize(args);
	}
}