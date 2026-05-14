using System.Diagnostics.CodeAnalysis;
using OpenTK.Mathematics;

namespace Engine3.Client.Graphics;

public readonly record struct Material {
	public required Color4<Rgba> Color { get; init; }

	[SetsRequiredMembers] public Material(Color4<Rgba> color) => Color = color;
}