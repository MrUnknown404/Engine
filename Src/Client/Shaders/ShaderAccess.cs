using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using USharpLibs.Common.IO;
using USharpLibs.Engine2.Debug;

namespace USharpLibs.Engine2.Client.Shaders {
	[PublicAPI]
	public class ShaderAccess {
		internal Shader Shader { private get; init; } = null!; // Set in Shader<T>

		protected void SetUniform<V>(string name, V data, SetUniformDelegate<V> uniformFunc) where V : struct {
			if (ShaderErrorHandler.Assert(Shader.Handle == 0, () => new(Shader, ShaderErrorHandler.Reason.NoHandle))) { return; }
			if (ShaderAccessErrorHandler.Assert(GLH.CurrentShaderHandle != Shader.Handle, () => new(Shader, ShaderAccessErrorHandler.Reason.NoLongerValid))) { return; }

			if (!Shader.UniformLocations.TryGetValue(name, out int value)) {
				Logger.Warn($"Tried to set variable named '{name}' in shader '{Shader.DebugName}' but it doesn't exist!");
				return;
			}

			uniformFunc(value, data);
		}

		protected void SetMatrix(string name, bool flag, Matrix4 data) {
			if (ShaderErrorHandler.Assert(Shader.Handle == 0, () => new(Shader, ShaderErrorHandler.Reason.NoHandle))) { return; }
			if (ShaderAccessErrorHandler.Assert(GLH.CurrentShaderHandle != Shader.Handle, () => new(Shader, ShaderAccessErrorHandler.Reason.NoLongerValid))) { return; }

			if (!Shader.UniformLocations.TryGetValue(name, out int value)) {
				Logger.Warn($"Tried to set variable named '{name}' in shader '{Shader.DebugName}' but it doesn't exist!");
				return;
			}

			GL.UniformMatrix4(value, flag, ref data);
		}

		protected void SetMatrix4Array(string name, bool flag, Matrix4[] data) {
			if (ShaderErrorHandler.Assert(Shader.Handle == 0, () => new(Shader, ShaderErrorHandler.Reason.NoHandle))) { return; }
			if (ShaderAccessErrorHandler.Assert(GLH.CurrentShaderHandle != Shader.Handle, () => new(Shader, ShaderAccessErrorHandler.Reason.NoLongerValid))) { return; }

			if (!Shader.UniformLocations.TryGetValue(name, out int value)) {
				Logger.Warn($"Tried to set variable named '{name}' in shader '{Shader.DebugName}' but it doesn't exist!");
				return;
			}

			GL.UniformMatrix4(value, data.Length, flag, ref data[0].Row0.X);
		}

		public void SetProjection(in Matrix4 data) => SetMatrix4("Projection", data);

		public void SetBool(string name, bool data) => SetUniform(name, data ? 1 : 0, GL.Uniform1);
		public void SetInt(string name, int data) => SetUniform(name, data, GL.Uniform1);
		public void SetFloat(string name, float data) => SetUniform(name, data, GL.Uniform1);
		public void SetVector2(string name, Vector2 data) => SetUniform(name, data, GL.Uniform2);
		public void SetVector3(string name, Vector3 data) => SetUniform(name, data, GL.Uniform3);
		public void SetVector4(string name, Vector4 data) => SetUniform(name, data, GL.Uniform4);
		public void SetMatrix4(string name, in Matrix4 data) => SetMatrix(name, true, data);
		public void SetMatrix4Array(string name, in Matrix4[] data) => SetMatrix4Array(name, true, data);
		public void SetColor(string name, Color4 data) => SetUniform(name, data, GL.Uniform4);

		protected delegate void SetUniformDelegate<in V>(int uniform, V value) where V : struct;
	}
}