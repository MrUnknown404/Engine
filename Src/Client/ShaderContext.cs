using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine3.Client {
	public class ShaderContext {
		internal Shader Shader { private get; init; } = null!; // Set in Shader<T>

		protected void SetUniform<V>(string name, V data, SetUniformDelegate<V> uniformFunc) where V : struct {
			// TODO check

			// if (ShaderErrorHandler.Assert(Shader.Handle == 0, () => new(Shader, ShaderErrorHandler.Reason.NoHandle))) { return; }
			// if (ShaderAccessErrorHandler.Assert(GLH.CurrentShaderHandle != Shader.Handle, () => new(Shader, ShaderAccessErrorHandler.Reason.NoLongerValid))) { return; }

			if (Shader.TryGetUniform(name, out int location)) { uniformFunc(location, data); }
		}

		protected void SetMatrix(string name, bool flag, Matrix4 data) {
			// TODO check

			// if (ShaderErrorHandler.Assert(Shader.Handle == 0, () => new(Shader, ShaderErrorHandler.Reason.NoHandle))) { return; }
			// if (ShaderAccessErrorHandler.Assert(GLH.CurrentShaderHandle != Shader.Handle, () => new(Shader, ShaderAccessErrorHandler.Reason.NoLongerValid))) { return; }

			if (Shader.TryGetUniform(name, out int location)) { GL.UniformMatrix4(location, flag, ref data); }
		}

		protected void SetMatrix4Array(string name, bool flag, Matrix4[] data) {
			// TODO check

			// if (ShaderErrorHandler.Assert(Shader.Handle == 0, () => new(Shader, ShaderErrorHandler.Reason.NoHandle))) { return; }
			// if (ShaderAccessErrorHandler.Assert(GLH.CurrentShaderHandle != Shader.Handle, () => new(Shader, ShaderAccessErrorHandler.Reason.NoLongerValid))) { return; }

			if (Shader.TryGetUniform(name, out int location)) { GL.UniformMatrix4(location, data.Length, flag, ref data[0].Row0.X); }
		}

		public void SetProjection(in Matrix4 data) => SetMatrix4("Projection", data);

		public void SetBool(string name, bool data) => SetUniform(name, data ? 1 : 0, GL.Uniform1);
		public void SetInt(string name, int data) => SetUniform(name, data, GL.Uniform1);
		public void SetFloat(string name, float data) => SetUniform(name, data, GL.Uniform1);
		public void SetVector2(string name, in Vector2 data) => SetUniform(name, data, GL.Uniform2);
		public void SetVector3(string name, in Vector3 data) => SetUniform(name, data, GL.Uniform3);
		public void SetVector4(string name, in Vector4 data) => SetUniform(name, data, GL.Uniform4);
		public void SetMatrix4(string name, in Matrix4 data) => SetMatrix(name, true, data);
		public void SetMatrix4Array(string name, in Matrix4[] data) => SetMatrix4Array(name, true, data);
		public void SetColor(string name, in Color4 data) => SetUniform(name, data, GL.Uniform4);

		protected delegate void SetUniformDelegate<in V>(int uniform, V value) where V : struct;
	}
}