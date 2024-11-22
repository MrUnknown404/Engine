using System.Text;
using JetBrains.Annotations;

namespace USharpLibs.Engine2.Client.GL.Models.Vertex {
	/// <summary> TODO write this </summary>
	/// <example> <see cref="Vertex3"/> or <see cref="Vertex5"/> </example>
	[PublicAPI]
	public interface IVertex {
		/// <summary> This is how the float data is arranged. </summary>
		public static abstract byte[] Arrangement { get; }
		/// <summary> This is the total count of floats in the struct. </summary>
		public static abstract byte Length { get; }

		public void Collect(ref float[] array, int startIndex, byte length) {
			for (byte i = 0; i < length; i++) { array[startIndex + i] = this[i]; }
		}

		public float this[byte i] { get; }
	}

	public static class VertexExtensions {
		public static string GenerateString<T>(this T self) where T : IVertex {
			StringBuilder sb = new("(");

			float[] array = new float[T.Length];
			self.Collect(ref array, 0, T.Length);
			foreach (float var in array) { sb.Append($"{var}, "); }

			sb.Length -= 2;
			sb.Append(')');
			return sb.ToString();
		}
	}
}