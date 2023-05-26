using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using OpenTK.Mathematics;
using USharpLibs.Engine.Client.GL.Models;

namespace USharpLibs.Engine.Client.GL.Shapes2D {
	[PublicAPI]
	public static class Quads {
		/// <summary> Creates a 2D <see cref="Mesh"/> that contains a quad created from the provided values. <br/>
		/// The Vertical parameters (Y, H) are assuming a vertically off-centered orthographic projection matrix. <br/>
		/// <br/>
		/// An example of a vertically off-centered orthographic projection matrix is below.
		/// </summary>
		///
		/// <example> An example of a correct orthographic projection matrix.
		/// 	<code> Matrix4.CreateOrthographicOffCenter(0, Width, Height, 0, Near, Far))) </code>
		/// </example>
		///
		/// <seealso cref="Mesh"/>
		/// <seealso cref="Matrix4.CreateOrthographicOffCenter(float, float, float, float, float, float, out Matrix4)"> Matrix4.CreateOrthographicOffCenter() </seealso>
		///
		/// <param name="w"> The Width of the quad. </param>
		/// <param name="h"> The Height of the quad. </param>
		/// <param name="z"> The Z coordinate. </param>
		public static Mesh WH(float w, float h, float z = 0) => WH(0, 0, w, h, z, 0, 0, 1, 1);

		/// <inheritdoc cref="WH(float, float, float)"/>
		/// <param name="u0"> The top left texture coordinate. </param>
		/// <param name="v0"> The top right texture coordinate. </param>
		/// <param name="u1"> The bottom left texture coordinate. </param>
		/// <param name="v1"> The bottom right texture coordinate. </param>
		[SuppressMessage("ReSharper", "InvalidXmlDocComment")]
		public static Mesh WH(float w, float h, float z, float u0, float v0, float u1, float v1) => Raw(0, h, w, 0, z, u0, v0, u1, v1);

		/// <param name="x"> The X coordinate. </param>
		/// <param name="y"> The Y coordinate. </param>
		/// <inheritdoc cref="WH(float, float, float)"/>
		[SuppressMessage("ReSharper", "InvalidXmlDocComment")]
		public static Mesh WH(float x, float y, float w, float h, float z = 0) => WH(x, y, w, h, z, 0, 0, 1, 1);

		/// <param name="x"> The X coordinate. </param>
		/// <param name="y"> The Y coordinate. </param>
		/// <inheritdoc cref="WH(float, float, float, float, float, float, float)"/>
		[SuppressMessage("ReSharper", "InvalidXmlDocComment")]
		public static Mesh WH(float x, float y, float w, float h, float z, float u0, float v0, float u1, float v1) => Raw(x, y + h, x + w, y, z, u0, v0, u1, v1);

		// @formatter:off
		private static Mesh Raw(float x0, float y0, float x1, float y1, float z, float u0, float v0, float u1, float v1) => new(new[] {
				x0, y0, z, u0, v0,
				x0, y1, z, u0, v1,
				x1, y0, z, u1, v0,
				x1, y1, z, u1, v1,
		}, new uint[] { 0, 2, 1, 2, 3, 1, });
		// @formatter:on
	}
}