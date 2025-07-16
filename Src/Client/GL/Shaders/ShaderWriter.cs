using JetBrains.Annotations;
using OpenTK.Mathematics;
using USharpLibs.Common.IO;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine.Client.GL.Shaders {
	[PublicAPI]
	public class ShaderWriter : IDisposable {
		protected internal int OriginalShaderHandle { protected get; init; }

		protected static Dictionary<string, int>? UniformLocations => GLH.CurrentShaderUniformLocations;

		protected void SetData<V>(string name, V data, Action<int, V> apply) {
			if (GLH.CurrentShaderHandle != OriginalShaderHandle) { Logger.Warn("Attempted to modify an unbound shader."); }
			if (UniformLocations == null) { throw new NullReferenceException("Somehow you tried to modify a shader that is both bound and unbound"); }
			if (!UniformLocations.TryGetValue(name, out int value)) {
				Logger.Warn($"Tried to set variable named '{name}' in shader '{GLH.CurrentShaderName}' but it doesn't exist!");
				return;
			}

			apply(value, data);
		}

		protected void SetMatrix(string name, bool flag, Matrix4 data) {
			if (GLH.CurrentShaderHandle != OriginalShaderHandle) { Logger.Warn("Attempted to modify an unbound shader."); }
			if (UniformLocations == null) { throw new NullReferenceException("Somehow you tried to modify a shader that is both bound and unbound"); }
			if (!UniformLocations.TryGetValue(name, out int value)) {
				Logger.Warn($"Tried to set variable named '{name}' in shader '{GLH.CurrentShaderName}' but it doesn't exist!");
				return;
			}

			OpenGL4.UniformMatrix4(value, flag, ref data);
		}

		protected void SetMatrix4Array(string name, bool flag, Matrix4[] datas) {
			if (GLH.CurrentShaderHandle != OriginalShaderHandle) { Logger.Warn("Attempted to modify an unbound shader."); }
			if (UniformLocations == null) { throw new NullReferenceException("Somehow you tried to modify a shader that is both bound and unbound"); }
			if (!UniformLocations.TryGetValue(name, out int value)) {
				Logger.Warn($"Tried to set variable named '{name}' in shader '{GLH.CurrentShaderName}' but it doesn't exist!");
				return;
			}

			OpenGL4.UniformMatrix4(value, datas.Length, flag, ref datas[0].Row0.X);
		}

		public void SetProjection(Matrix4 data) => SetMatrix4("Projection", data);

		public void SetBool(string name, bool data) => SetData(name, data ? 1 : 0, OpenGL4.Uniform1);
		public void SetInt(string name, int data) => SetData(name, data, OpenGL4.Uniform1);
		public void SetFloat(string name, float data) => SetData(name, data, OpenGL4.Uniform1);
		public void SetVector2(string name, Vector2 data) => SetData(name, data, OpenGL4.Uniform2);
		public void SetVector3(string name, Vector3 data) => SetData(name, data, OpenGL4.Uniform3);
		public void SetVector4(string name, Vector4 data) => SetData(name, data, OpenGL4.Uniform4);
		public void SetMatrix4(string name, Matrix4 data) => SetMatrix(name, true, data);
		public void SetMatrix4Array(string name, Matrix4[] data) => SetMatrix4Array(name, true, data);
		public void SetColor(string name, Color4 data) => SetData(name, data, OpenGL4.Uniform4);

		// This isn't empty tho???
#pragma warning disable CA1821
		~ShaderWriter() => throw new("ShaderAccess was not used correctly. No i will not explain.");
#pragma warning restore CA1821

		[Obsolete("Use 'Using'")] public void Dispose() => GC.SuppressFinalize(this);
	}
}