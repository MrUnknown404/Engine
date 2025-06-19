using Engine3.Utils;

namespace Engine3.Client {
	public class OpenGLException : Exception, IReasonEnumException<OpenGLException.Reason> {
		public Reason ReasonRef { get; }

		public OpenGLException(Reason reasonRef) : base(reasonRef switch {
				Reason.UnknownError => "Unknown OpenGL error. panic?",
				_ => throw new ArgumentOutOfRangeException(nameof(reasonRef), reasonRef, null),
		}) => ReasonRef = reasonRef;

		public enum Reason : byte {
			UnknownError = 0,
		}
	}
}