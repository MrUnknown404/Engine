using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine.Client.GL {
	[PublicAPI]
	public class ExampleShader : Shader {
		public Matrix4 Projection { get; set; }

		public ExampleShader(string vertName, string fragName, Matrix4 projection) : base(vertName, fragName) => Projection = projection;
		public ExampleShader(string name, Matrix4 projection) : this(name, name, projection) { }

		protected override void ISetupGL() {
			CompileShader(ShaderType.VertexShader, VertName, out int vertexShader);
			CompileShader(ShaderType.FragmentShader, FragName, out int fragmentShader);

			Handle = OpenGL4.CreateProgram();

			OpenGL4.AttachShader(Handle, vertexShader);
			OpenGL4.AttachShader(Handle, fragmentShader);

			OpenGL4.LinkProgram(Handle);
			OpenGL4.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int code);
			if (code != (int)All.True) { throw new($"Error occurred whilst linking Shader({Handle})"); }

			OpenGL4.DetachShader(Handle, vertexShader);
			OpenGL4.DetachShader(Handle, fragmentShader);
			OpenGL4.DeleteShader(fragmentShader);
			OpenGL4.DeleteShader(vertexShader);

			OpenGL4.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out int numberOfUniforms);
			for (int i = 0; i < numberOfUniforms; i++) {
				string key = OpenGL4.GetActiveUniform(Handle, i, out _, out _);
				UniformLocations.Add(key, OpenGL4.GetUniformLocation(Handle, key));
			}

			OpenGL4.UseProgram(Handle);
			SetProjection(Projection);
		}

		public void SetProjection(Matrix4 data) => SetMatrix4("Projection", Projection = data);
	}
}