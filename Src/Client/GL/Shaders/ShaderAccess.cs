using JetBrains.Annotations;
using OpenTK.Mathematics;
using USharpLibs.Common.IO;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine2.Client.GL.Shaders {
	[PublicAPI]
	public class ShaderAccess {
		internal Shader Shader { private get; init; } = null!; // Set in Shader<T>

		protected void CheckIfValid() {
			if (Shader.Handle == 0) { throw new ShaderAccessException(ShaderAccessException.Reason.NeverRegistered); }
			if (GLH.CurrentShaderHandle != Shader.Handle) { throw new ShaderAccessException(ShaderAccessException.Reason.NoLongerValid); }
		}

		protected void SetData<V>(string name, V data, Action<int, V> apply) {
			CheckIfValid();

			if (!Shader.UniformLocations.TryGetValue(name, out int value)) {
				Logger.Warn($"Tried to set variable named '{name}' in shader '{Shader.DebugName}' but it doesn't exist!");
				return;
			}

			apply(value, data);
		}

		protected void SetMatrix(string name, bool flag, Matrix4 data) {
			CheckIfValid();

			if (!Shader.UniformLocations.TryGetValue(name, out int value)) {
				Logger.Warn($"Tried to set variable named '{name}' in shader '{Shader.DebugName}' but it doesn't exist!");
				return;
			}

			OpenGL4.UniformMatrix4(value, flag, ref data);
		}

		protected void SetMatrix4Array(string name, bool flag, Matrix4[] data) {
			CheckIfValid();

			if (!Shader.UniformLocations.TryGetValue(name, out int value)) {
				Logger.Warn($"Tried to set variable named '{name}' in shader '{Shader.DebugName}' but it doesn't exist!");
				return;
			}

			OpenGL4.UniformMatrix4(value, data.Length, flag, ref data[0].Row0.X);
		}

		public void SetProjection(in Matrix4 data) => SetMatrix4("Projection", data);

		public void SetBool(string name, bool data) => SetData(name, data ? 1 : 0, OpenGL4.Uniform1);
		public void SetInt(string name, int data) => SetData(name, data, OpenGL4.Uniform1);
		public void SetFloat(string name, float data) => SetData(name, data, OpenGL4.Uniform1);
		public void SetVector2(string name, Vector2 data) => SetData(name, data, OpenGL4.Uniform2);
		public void SetVector3(string name, Vector3 data) => SetData(name, data, OpenGL4.Uniform3);
		public void SetVector4(string name, Vector4 data) => SetData(name, data, OpenGL4.Uniform4);
		public void SetMatrix4(string name, in Matrix4 data) => SetMatrix(name, true, data);
		public void SetMatrix4Array(string name, in Matrix4[] data) => SetMatrix4Array(name, true, data);
		public void SetColor(string name, Color4 data) => SetData(name, data, OpenGL4.Uniform4);
	}
}