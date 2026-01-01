using System.Numerics;

namespace Engine3.Api.Graphics {
	[Obsolete]
	public interface IShaderAccess {
		public void SetUniform(string name, bool value);
		public void SetUniform(string name, int value);
		public void SetUniform(string name, float value);
		public void SetUniform(string name, Vector2 value);
		public void SetUniform(string name, Vector3 value);
		public void SetUniform(string name, Vector4 value);
		public void SetUniform(string name, Matrix4x4 value);
	}
}