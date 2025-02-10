using USharpLibs.Engine2.Exceptions;

namespace USharpLibs.Engine2.Debug {
	public class ModelAccessErrorHandler : ErrorHandler<ModelAccessErrorHandler, ModelAccessException>, IErrorHandler<ModelAccessErrorHandler, ModelAccessException> {
		public static ErrorHandleMode ErrorMode { get; set; } = ErrorHandleMode.Throw;

		private Reason ReasonValue { get; }

		public ModelAccessErrorHandler(Reason reasonValue) => ReasonValue = reasonValue;

		public static string CreateMessage(ModelAccessErrorHandler error) =>
				error.ReasonValue switch {
						Reason.NothingBound => "Cannot use ModelAccess methods when no model is bound.",
						Reason.NothingToBuild => "Model contains no data to build.",
						Reason.NothingToDraw => "Model contains no data to draw.",
						_ => throw new ArgumentOutOfRangeException(),
				};

		public static ModelAccessException CreateException(ModelAccessErrorHandler error) => new(CreateMessage(error));

		public enum Reason : byte {
			NothingBound = 0,
			NothingToBuild,
			NothingToDraw,
		}
	}
}