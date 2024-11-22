using JetBrains.Annotations;

namespace USharpLibs.Engine2.Engine {
	[PublicAPI]
	public class EngineStateException : Exception {
		public Reason ExceptionReason { get; }

		public EngineStateException(Reason reason) : base(ReasonToString(reason)) => ExceptionReason = reason;

		private static string ReasonToString(Reason r) =>
				r switch {
						Reason.EngineStartNotCalled => "GameEngine#Start must be called before accessing this.",
						Reason.EngineStartAlreadyCalled => "GameEngine#Start cannot be called twice.",
						_ => throw new ArgumentOutOfRangeException(nameof(r), r, null),
				};

		public enum Reason : byte {
			EngineStartNotCalled = 0,
			EngineStartAlreadyCalled,
		}
	}
}