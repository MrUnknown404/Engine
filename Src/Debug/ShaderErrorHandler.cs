using USharpLibs.Engine2.Client.Shaders;
using USharpLibs.Engine2.Exceptions;

namespace USharpLibs.Engine2.Debug {
	public class ShaderErrorHandler : ErrorHandler<ShaderErrorHandler, ShaderException>, IErrorHandler<ShaderErrorHandler, ShaderException> {
		public static ErrorHandleMode ErrorMode { get; set; } = ErrorHandleMode.Throw;

		private Shader Shader { get; }
		private Reason ReasonValue { get; }

		public ShaderErrorHandler(Shader shader, Reason reasonValue) {
			Shader = shader;
			ReasonValue = reasonValue;
		}

		public static string CreateMessage(ShaderErrorHandler error) =>
				error.ReasonValue switch {
						Reason.NoHandle => $"Shader has no Handle. Was Shader registered? Shader: {error.Shader.DebugName}.",
						Reason.EmptyShaderTypes => $"ShaderTypes cannot be empty. Shader: {error.Shader.DebugName}.",
						Reason.LinkError => $"Error occurred whilst linking Shader: {error.Shader.DebugName}.",
						Reason.CompileError => $"Error occurred whilst compiling Shader: {error.Shader.DebugName}.",
						_ => throw new ArgumentOutOfRangeException(),
				};

		public static ShaderException CreateException(ShaderErrorHandler error) => new(CreateMessage(error));

		public enum Reason : byte {
			NoHandle = 0,
			EmptyShaderTypes,
			LinkError,
			CompileError,
		}
	}
}