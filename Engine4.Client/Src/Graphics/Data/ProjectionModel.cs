using System.Numerics;

namespace Engine4.Client.Graphics.Data;

public readonly record struct ProjectionModel {
	public Matrix4x4 Projection { get; init; } = Matrix4x4.Identity;
	public Matrix4x4 Model { get; init; } = Matrix4x4.Identity;

	public ProjectionModel(Matrix4x4 projection, Matrix4x4 model) {
		Model = model;
		Projection = projection;
	}
}