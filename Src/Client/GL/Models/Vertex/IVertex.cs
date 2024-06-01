using System.Collections;
using System.Text;
using JetBrains.Annotations;

namespace USharpLibs.Engine.Client.GL.Models.Vertex {
	/// <summary> TODO write this </summary>
	/// <example> <see cref="Vertex3"/> or <see cref="Vertex5"/> </example>
	[PublicAPI]
	public interface IVertex : IEnumerable<float> {
		/// <summary> This is how the float data is arranged. </summary>
		/// <example> <see cref="Vertex5"/> </example>
		public static abstract byte[] Arrangement { get; }
		/// <summary> This is the total count of floats in the struct. </summary>
		/// <example> <see cref="Vertex5"/> </example>
		public static abstract byte TotalSize { get; }

		// <summary>  </summary>

		/// <summary> This should collects all the struct's floats into the provided list. </summary>
		/// <remarks> If performance matters, use this. Otherwise, iterate over this instead. </remarks>
		/// <example> <see cref="Vertex5"/> </example>
		[Obsolete("This is an extra performant version of collection used in Model#ProcessMeshesIntoVertexArray. You probably don't need this")]
		public void Collect(ICollection<float> list);

		/// <summary> Return <see cref="Arrangement"/> </summary>
		/// <example> <see cref="Vertex5"/> </example>
		public byte[] GetArrangement(); // i don't like this but i can't figure out a better way

		/// <summary> Return <see cref="TotalSize"/> </summary>
		/// <example> <see cref="Vertex5"/> </example>
		public byte GetTotalSize();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public string GenerateString() {
			StringBuilder sb = new("(");

			foreach (float var in this) { sb.Append($"{var}, "); }

			sb.Length -= 2;
			sb.Append(')');
			return sb.ToString();
		}
	}
}