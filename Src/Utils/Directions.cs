using JetBrains.Annotations;
using OpenTK.Mathematics;

namespace USharpLibs.Engine.Utils {
	public enum Direction2d : byte {
		Up = 0b0001,
		Left = 0b0010,
		Down = 0b0100,
		Right = 0b1000,
	}

	public enum Direction3d : byte {
		North = 0,
		East,
		South,
		West,
		Up,
		Down,
	}

	[PublicAPI]
	public static class DirectionExtensions {
		public static Vector3i Offset(this Direction3d self) =>
				self switch {
						Direction3d.North => -Vector3i.UnitZ,
						Direction3d.East => Vector3i.UnitX,
						Direction3d.South => Vector3i.UnitZ,
						Direction3d.West => -Vector3i.UnitX,
						Direction3d.Up => Vector3i.UnitY,
						Direction3d.Down => -Vector3i.UnitY,
						_ => throw new NotImplementedException(),
				};
	}
}