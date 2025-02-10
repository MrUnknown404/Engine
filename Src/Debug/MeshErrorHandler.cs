using USharpLibs.Engine2.Exceptions;

namespace USharpLibs.Engine2.Debug {
	public class MeshErrorHandler : ErrorHandler<MeshErrorHandler, MeshException>, IErrorHandler<MeshErrorHandler, MeshException> {
		public static ErrorHandleMode ErrorMode { get; set; } = ErrorHandleMode.Throw;

		private Reason ReasonValue { get; }

		public MeshErrorHandler(Reason reasonValue) => ReasonValue = reasonValue;

		public static string CreateMessage(MeshErrorHandler error) =>
				error.ReasonValue switch {
						Reason.EmptyVertexArray => "Vertex array cannot be empty.",
						Reason.EmptyIndexArray => "Index array cannot be empty.",
						Reason.IncorrectlySizedIndexArray => "Index array must be divisible by 3.",
						Reason.IncorrectlySizedVertexArray => "Vertex array length must be the same.",
						_ => throw new ArgumentOutOfRangeException(),
				};

		public static MeshException CreateException(MeshErrorHandler error) => new(CreateMessage(error));

		public enum Reason : byte {
			EmptyVertexArray = 0,
			EmptyIndexArray,
			IncorrectlySizedVertexArray,
			IncorrectlySizedIndexArray,
		}
	}
}