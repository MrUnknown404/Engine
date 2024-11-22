using JetBrains.Annotations;

namespace USharpLibs.Engine2.Client.GL.Models {
	[PublicAPI]
	public class ModelException : Exception {
		public Reason ReasonValue { get; }

		public ModelException(Reason reason) : base(ReasonToString(reason)) => ReasonValue = reason;

		private static string ReasonToString(Reason reason) =>
				reason switch {
						Reason.WasFreed => "Cannot use any OpenGL related methods with a freed model.",
						Reason.VAOIsFinal => "Attempted to regenerate VAO. This is not supported.",
						Reason.ModelAlreadyFreed => "Cannot free model because it was already freed.",
						_ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
				};

		public enum Reason : byte {
			WasFreed = 0,
			VAOIsFinal,
			ModelAlreadyFreed,
		}
	}
}