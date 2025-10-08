using System.Numerics;
using OpenTK.Graphics;

namespace Engine3.Api.Graphics {
	public interface IShaderAccess {
		public ShaderHandle Handle { get; }
		public bool HasHandle { get; }

		public void SetUniform(string name, bool value);
		public void SetUniform(string name, int value);
		public void SetUniform(string name, float value);
		public void SetUniform(string name, Vector2 value);
		public void SetUniform(string name, Vector3 value);
		public void SetUniform(string name, Vector4 value);
		public void SetUniform(string name, Matrix4x4 value);
	}
}