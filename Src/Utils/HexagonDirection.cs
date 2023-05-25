namespace USharpLibs.Engine.Utils {
	public enum FlatHexagonDirection : byte {
		North,
		NorthEast,
		SouthEast,
		South,
		SouthWest,
		NorthWest,
	}

	public enum PointyHexagonDirection : byte {
		East,
		NorthEast,
		NorthWest,
		West,
		SouthWest,
		SouthEast,
	}

	public static class HexagonDirectionExtensions {
		public static HexPos ToOffsetPos(this FlatHexagonDirection self) => HexPos.FlatDirections[(byte)self];
		public static HexPos ToOffsetPos(this PointyHexagonDirection self) => HexPos.PointyDirections[(byte)self];
	}
}