using JetBrains.Annotations;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine.Client.GL {
	[PublicAPI]
	public class DefaultHudShader : ExampleShader {
		public DefaultHudShader(string vertName, string fragName) : base(vertName, fragName, Matrix4.Identity) { }
		public DefaultHudShader(string name) : this(name, name) { }

		protected internal override void OnResize(ResizeEventArgs args) {
			void UncheckedSetProjection(Matrix4 projection) => OpenGL4.UniformMatrix4(UniformLocations["Projection"], true, ref projection); // Ugly fix. because OpenGL4#UniformMatrix4 uses 'ref'

			OpenGL4.UseProgram(Handle);
			UncheckedSetProjection(Projection = Matrix4.CreateOrthographicOffCenter(0, args.Width, args.Height, 0, -10, 10));
			GLH.UnbindShader(true);
		}
	}
}