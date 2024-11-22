using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace USharpLibs.Engine2.Client.GL.Models.Vertex {
	[PublicAPI]
	[StructLayout(LayoutKind.Sequential, Pack = 0)]
	public readonly record struct Vertex3 : IVertex {
		public static byte[] Arrangement { get; } = new byte[] { 3, };
		public static byte Length => 3;

		public float X { get; } // 0-4
		public float Y { get; } // 4-8
		public float Z { get; } // 8-12

		public Vertex3(float x, float y, float z) {
			X = x;
			Y = y;
			Z = z;
		}

		public static implicit operator Vertex3((float x, float y, float z) vert) => new(vert.x, vert.y, vert.z);

		public void Deconstruct(out float x, out float y, out float z) {
			x = X;
			y = Y;
			z = Z;
		}

		public float this[byte i] =>
				i switch {
						0 => X,
						1 => Y,
						2 => Z,
						_ => throw new ArgumentOutOfRangeException(nameof(i), i, null),
				};

		public override string ToString() => this.GenerateString();
	}
}