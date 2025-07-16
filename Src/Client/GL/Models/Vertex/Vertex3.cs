using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace USharpLibs.Engine.Client.GL.Models.Vertex {
	[PublicAPI]
	[StructLayout(LayoutKind.Sequential, Pack = 0)]
	public readonly record struct Vertex3 : IVertex {
		public static byte[] Arrangement => new byte[] { 3, };
		public static byte TotalSize => 3;

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

		public void Collect(ICollection<float> list) {
			list.Add(X);
			list.Add(Y);
			list.Add(Z);
		}

		public byte GetTotalSize() => TotalSize;
		public byte[] GetArrangement() => Arrangement;

		public IEnumerator<float> GetEnumerator() {
			yield return X;
			yield return Y;
			yield return Z;
		}

		public override string ToString() => ((IVertex)this).GenerateString();
	}
}