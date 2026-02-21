using System.Numerics;

namespace Engine3.Client.Graphics.DataStructs {
	public readonly record struct ProjectionView {
		public Matrix4x4 Projection { get; init; } = Matrix4x4.Identity;
		public Matrix4x4 View { get; init; } = Matrix4x4.Identity;

		public ProjectionView(Matrix4x4 projection, Matrix4x4 view) {
			Projection = projection;
			View = view;
		}
	}
}