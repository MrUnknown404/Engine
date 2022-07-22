using OpenTK.Mathematics;

namespace USharpLibs.Engine.Utils {
	public enum Direction {
		North = 0,
		East,
		South,
		West,
		Up,
		Down,
	}

	public static class DirectionExtensions {
		public static Vector3i Offset(this Direction self) => self switch {
			Direction.North => -Vector3i.UnitZ,
			Direction.East => Vector3i.UnitX,
			Direction.South => Vector3i.UnitZ,
			Direction.West => -Vector3i.UnitX,
			Direction.Up => Vector3i.UnitY,
			Direction.Down => -Vector3i.UnitY,
			_ => throw new NotImplementedException(),
		};
	}
}