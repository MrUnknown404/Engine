using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using USharpLibs.Engine.Utils;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine.Client.GL {
	public abstract class RawShader {
		protected Dictionary<string, int> UniformLocations { get; private set; } = new();

		public int Handle { get; protected set; }
		protected string VertName { get; }
		protected string FragName { get; }

		protected RawShader(string vertName, string fragName) {
			VertName = vertName;
			FragName = fragName;
		}

		internal void SetupGL() {
			if (ClientBase.LoadState != LoadState.GL) { throw new Exception($"Cannot setup shader during {ClientBase.LoadState}"); }
			ISetupGL();
		}

		protected abstract void ISetupGL();

		protected static void CompileShader(ShaderType type, string name, out int shader) {
			OpenGL4.ShaderSource(shader = OpenGL4.CreateShader(type), File.ReadAllText($"Assets/Shaders/{name}.{type.ToFileFormat<ShaderType>()}"));
			OpenGL4.CompileShader(shader);
			OpenGL4.GetShader(shader, ShaderParameter.CompileStatus, out int code);
			if (code != (int)All.True) {
				throw new Exception($"Error occurred whilst compiling Shader({shader}).\n\n{OpenGL4.GetShaderInfoLog(shader)}");
			}
		}

		protected void SetData<V>(string name, V data, Action<int, V> apply) {
			if (GLH.CurrentShader != Handle && ClientBase.LoadState != LoadState.GL) {
				ClientBase.Logger.WarnLine("Trying to use an unbound shader!");
				return;
			}

			apply(UniformLocations[name], data);
		}

		protected void SetMatrix(string name, bool flag, Matrix4 data) {
			if (GLH.CurrentShader != Handle && ClientBase.LoadState != LoadState.GL) {
				ClientBase.Logger.WarnLine("Trying to use an unbound shader!");
				return;
			}

			OpenGL4.UniformMatrix4(UniformLocations[name], flag, ref data);
		}

		public void SetInt(string name, int data) => SetData(name, data, OpenGL4.Uniform1);
		public void SetFloat(string name, float data) => SetData(name, data, OpenGL4.Uniform1);
		public void SetVector2(string name, Vector2 data) => SetData(name, data, OpenGL4.Uniform2);
		public void SetVector3(string name, Vector3 data) => SetData(name, data, OpenGL4.Uniform3);
		public void SetVector4(string name, Vector4 data) => SetData(name, data, OpenGL4.Uniform4);
		public void SetMatrix4(string name, Matrix4 data) => SetMatrix(name, true, data);
		public void SetColor(string name, Color4 data) => SetData(name, data, OpenGL4.Uniform4);
	}
}