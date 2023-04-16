using JetBrains.Annotations;
using OpenTK.Mathematics;

namespace USharpLibs.Engine.Client.GL.Model {
	[PublicAPI]
	public class Joint {
		public byte Index { get; }
		public string Name { get; } // TODO remove this
		public List<Joint> Children { get; } = new();

		public Matrix4 AnimatedTransform { get; set; }

		private Matrix4 LocalBindTransform { get; }
		public Matrix4 InverseBindTransform { get; private set; }

		public Joint(byte index, string name, Matrix4 localBindTransform) {
			Index = index;
			Name = name;
			LocalBindTransform = localBindTransform;
		}

		public void CalcInverseBindTransform(Matrix4 parentBindTransform) {
			Matrix4 bindTransform = parentBindTransform * LocalBindTransform;
			InverseBindTransform = bindTransform.Inverted();

			foreach (Joint child in Children) { child.CalcInverseBindTransform(bindTransform); }
		}
	}
}