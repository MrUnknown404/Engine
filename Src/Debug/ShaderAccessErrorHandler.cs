using USharpLibs.Engine2.Client.Shaders;
using USharpLibs.Engine2.Exceptions;

namespace USharpLibs.Engine2.Debug {
	public class ShaderAccessErrorHandler : ErrorHandler<ShaderAccessErrorHandler, ShaderAccessException>, IErrorHandler<ShaderAccessErrorHandler, ShaderAccessException> {
		public static ErrorHandleMode ErrorMode { get; set; } = ErrorHandleMode.Throw;

		private Shader Shader { get; }
		private Reason ReasonValue { get; }

		public ShaderAccessErrorHandler(Shader shader, Reason reasonValue) {
			Shader = shader;
			ReasonValue = reasonValue;
		}

		public static string CreateMessage(ShaderAccessErrorHandler error) =>
				error.ReasonValue switch {
						Reason.NoLongerValid => $"ShaderAccess is no longer valid. Shader: {error.Shader.DebugName}",
						_ => throw new ArgumentOutOfRangeException(),
				};

		public static ShaderAccessException CreateException(ShaderAccessErrorHandler error) => new(CreateMessage(error));

		public enum Reason : byte {
			NoLongerValid = 0,
		}
	}
}