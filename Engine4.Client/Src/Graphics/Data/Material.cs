using System.Diagnostics.CodeAnalysis;
using OpenTK.Mathematics;

namespace Engine4.Client.Graphics.Data;

public readonly record struct Material {
	public required Color4<Rgba> Color { get; init; }

	[SetsRequiredMembers]
	public Material(Color4<Rgba> color) => Color = color;
}