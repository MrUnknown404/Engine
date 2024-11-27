using USharpLibs.Engine2.Client.Models;

namespace USharpLibs.Engine2.Exceptions {
	[PublicAPI]
	public class ModelAccessException : Exception {
		public Reason ReasonValue { get; }

		public ModelAccessException(Reason reason) : base(ReasonToString(reason)) => ReasonValue = reason;

		private static string ReasonToString(Reason reason) =>
				reason switch {
						Reason.NothingBound => $"{nameof(ModelAccess)}#{nameof(ModelAccess.Build)}/{nameof(ModelAccess.Draw)} cannot be used when no model is bound.",
						Reason.NoVAO => "Cannot use any OpenGL related methods without a vao.",
						Reason.WasFreed => "Cannot use any OpenGL related methods with a freed model.",
						Reason.NothingToBuild => "Model contains no data to build.",
						Reason.NothingToDraw => "Model contains no data to draw.",
						_ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
				};

		public enum Reason : byte {
			NothingBound = 0,
			NoVAO,
			WasFreed,
			NothingToBuild,
			NothingToDraw,
		}
	}
}