using USharpLibs.Engine2.Exceptions;

namespace USharpLibs.Engine2.Debug {
	public class ModelErrorHandler : ErrorHandler<ModelErrorHandler, ModelException>, IErrorHandler<ModelErrorHandler, ModelException> {
		public static ErrorHandleMode ErrorMode { get; set; } = ErrorHandleMode.Throw;

		private Reason ReasonValue { get; }

		public ModelErrorHandler(Reason reasonValue) => ReasonValue = reasonValue;

		public static string CreateMessage(ModelErrorHandler error) =>
				error.ReasonValue switch {
						Reason.NoVAO => "Model has no VAO. was Model#GenerateVAO called?",
						Reason.VAOIsFinal => "Attempted to regenerate VAO. This is not supported.",
						Reason.WasFreed => "Cannot use any OpenGL related methods with a freed model.",
						Reason.ModelAlreadyFreed => "Cannot free model because it was already freed.",
						Reason.ImmutableModelAlreadyBuilt => "Attempted to build an already built immutable model.",
						_ => throw new ArgumentOutOfRangeException(),
				};

		public static ModelException CreateException(ModelErrorHandler error) => new(CreateMessage(error));

		public enum Reason {
			NoVAO = 0,
			VAOIsFinal,
			WasFreed,
			ModelAlreadyFreed,
			ImmutableModelAlreadyBuilt,
		}
	}
}